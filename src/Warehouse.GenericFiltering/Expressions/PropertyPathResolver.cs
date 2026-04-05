using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Warehouse.GenericFiltering;

/// <summary>
/// Discovers filterable property paths on entity types and generates selector lambda expressions.
/// <para>Uses BFS path discovery with lazy auto-loading via <see cref="ConcurrentDictionary{TKey,TValue}"/>.</para>
/// </summary>
internal static class PropertyPathResolver
{
    /// <summary>
    /// Represents a dot-separated property path with associated type metadata.
    /// </summary>
    internal sealed class PathWithTypesRecord
    {
        /// <summary>
        /// Gets the dot-separated property path.
        /// </summary>
        internal string Path { get; }

        /// <summary>
        /// Gets the collection element types along the path.
        /// </summary>
        internal Type[] Types { get; }

        /// <summary>
        /// Initializes a new instance with the specified path and type array.
        /// </summary>
        internal PathWithTypesRecord(string path, Type[] types)
        {
            Path = path;
            Types = types;
        }
    }

    /// <summary>
    /// Represents a node in the BFS path discovery queue.
    /// </summary>
    internal struct PathNode
    {
        /// <summary>
        /// The CLR type at this node.
        /// </summary>
        internal Type ClrType;

        /// <summary>
        /// The accumulated dot-separated path to this node.
        /// </summary>
        internal string Path;

        /// <summary>
        /// Whether to skip the visited-property guard for root-level properties.
        /// </summary>
        internal bool SkipVisitedGuard;

        /// <summary>
        /// Initializes a new instance with the specified type, path, and guard flag.
        /// </summary>
        internal PathNode(Type clrType, string path, bool skipVisitedGuard)
        {
            ClrType = clrType;
            Path = path;
            SkipVisitedGuard = skipVisitedGuard;
        }
    }

    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, LambdaExpression>> TypesSelectors = new();

    /// <summary>
    /// Returns the selector map for the entity type, auto-loading on first access.
    /// </summary>
    internal static ConcurrentDictionary<string, LambdaExpression> GetSelectors(Type entityType)
    {
        return TypesSelectors.GetOrAdd(entityType, LoadTypeInternal);
    }

    /// <summary>
    /// Finds the first generic <see cref="IEnumerable{T}"/> interface implemented by the type.
    /// </summary>
    internal static Type? FindGenericEnumerableInterface(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type;

        return type
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType
                                 && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    /// <summary>
    /// Loads a single entity type: discovers all paths via BFS, generates selectors, and resolves aliases.
    /// </summary>
    private static ConcurrentDictionary<string, LambdaExpression> LoadTypeInternal(Type entityType)
    {
        ConcurrentDictionary<string, LambdaExpression> selectorMap = new(StringComparer.OrdinalIgnoreCase);

        PathWithTypesRecord[] allPathRecords = GetAvailablePaths(entityType).ToArray();

        foreach (PathWithTypesRecord pathRecord in allPathRecords)
        {
            string fullPathKey = pathRecord.Path.ToLowerInvariant();
            LambdaExpression selector = GenerateSelector(entityType, pathRecord);
            selectorMap.AddOrUpdate(fullPathKey, selector, (_, _) => selector);
        }

        RegisterAliases(entityType, allPathRecords, selectorMap);

        return selectorMap;
    }

    /// <summary>
    /// Resolves <see cref="FilterAliasAttribute"/> on leaf properties and registers alias keys.
    /// </summary>
    private static void RegisterAliases(
        Type entityType,
        PathWithTypesRecord[] allPathRecords,
        ConcurrentDictionary<string, LambdaExpression> selectorMap)
    {
        foreach (PathWithTypesRecord pathRecord in allPathRecords)
        {
            PropertyInfo leafProp = ResolveLeafProperty(entityType, pathRecord.Path);

            FilterAliasAttribute[] allAliases = leafProp
                .GetCustomAttributes<FilterAliasAttribute>(false)
                .ToArray();

            FilterAliasAttribute[] matching = allAliases
                .Where(a => IsPathMatch(a, pathRecord.Path, leafProp.Name))
                .ToArray();

            if (matching.Length == 0)
                matching = allAliases.Where(a => string.IsNullOrEmpty(a.Path)).ToArray();

            if (matching.Length == 0)
                continue;

            LambdaExpression targetSelector = selectorMap[pathRecord.Path.ToLowerInvariant()];

            foreach (FilterAliasAttribute aliasAttr in matching)
            {
                string aliasKey = aliasAttr.Alias.ToLowerInvariant();
                selectorMap.AddOrUpdate(aliasKey, targetSelector, (_, _) => targetSelector);
            }
        }
    }

    /// <summary>
    /// Walks the dot-separated path to resolve the leaf <see cref="PropertyInfo"/>.
    /// </summary>
    private static PropertyInfo ResolveLeafProperty(Type entityType, string path)
    {
        string[] segments = path.Split(FilterConstants.PATH_SEPARATOR[0]);
        Type currentType = entityType;
        PropertyInfo leafProp = null!;

        foreach (string segment in segments)
        {
            leafProp = currentType.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance)
                       ?? throw new FilterException(
                           $"Cannot find property '{segment}' on '{currentType.Name}'.");

            Type? seqIface = FindGenericEnumerableInterface(leafProp.PropertyType);
            currentType = (seqIface is not null && leafProp.PropertyType != typeof(string))
                ? seqIface.GetGenericArguments()[0]
                : leafProp.PropertyType;
        }

        return leafProp;
    }

    /// <summary>
    /// Returns true when the alias attribute targets the same leaf path.
    /// </summary>
    private static bool IsPathMatch(
        FilterAliasAttribute aliasAttr,
        string fullLeafPath,
        string leafPropertyName)
    {
        if (string.IsNullOrEmpty(aliasAttr.Path))
            return false;

        string expected = aliasAttr.Path.EndsWith(leafPropertyName, StringComparison.OrdinalIgnoreCase)
            ? aliasAttr.Path
            : $"{aliasAttr.Path}{FilterConstants.PATH_SEPARATOR}{leafPropertyName}";

        return fullLeafPath.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Generates a lambda selector expression for the given entity type and path record.
    /// </summary>
    private static LambdaExpression GenerateSelector(Type rootEntityType, PathWithTypesRecord pathRecord)
    {
        ParameterExpression parameterExpression = Expression.Parameter(rootEntityType, FilterConstants.PARAMETER_NAME);
        string[] pathSegments = pathRecord.Path.Split(FilterConstants.PATH_SEPARATOR[0]);
        Expression selectorBody = BuildSelectorBody(parameterExpression, rootEntityType, pathSegments);
        return Expression.Lambda(selectorBody, parameterExpression);
    }

    /// <summary>
    /// Walks each path segment, switching between single-object and sequence modes.
    /// </summary>
    private static Expression BuildSelectorBody(
        Expression startingExpression,
        Type startingType,
        string[] pathSegments)
    {
        bool isInSequenceMode = false;
        Type currentElementType = startingType;
        Expression currentExpression = startingExpression;

        foreach (string segment in pathSegments)
        {
            if (!isInSequenceMode)
            {
                currentExpression = BuildPropertyOrCastStep(
                    currentExpression,
                    currentElementType,
                    segment,
                    out currentElementType,
                    out isInSequenceMode);

                continue;
            }

            currentExpression = BuildSequenceProjectionStep(
                currentExpression,
                currentElementType,
                segment,
                out currentElementType);
        }

        return currentExpression;
    }

    /// <summary>
    /// Handles a single-object step: accesses a property and casts to IEnumerable if collection.
    /// </summary>
    private static Expression BuildPropertyOrCastStep(
        Expression sourceExpression,
        Type sourceType,
        string propertyName,
        out Type resultingElementType,
        out bool resultingIsSequence)
    {
        PropertyInfo propInfo = sourceType.GetProperty(propertyName)
                                ?? throw new FilterException(
                                    $"Property '{propertyName}' not found on '{sourceType.Name}'.");

        Expression access = Expression.Property(sourceExpression, propInfo);

        Type propType = propInfo.PropertyType;
        Type? seqIface = FindGenericEnumerableInterface(propType);

        if (seqIface != null && propType != typeof(string))
        {
            resultingIsSequence = true;
            resultingElementType = seqIface.GetGenericArguments()[0];

            return Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Cast),
                [resultingElementType],
                access);
        }

        resultingIsSequence = false;
        resultingElementType = propType;
        return access;
    }

    /// <summary>
    /// Handles a sequence step: projects via Select or flattens via SelectMany.
    /// </summary>
    private static Expression BuildSequenceProjectionStep(
        Expression sequenceExpression,
        Type elementType,
        string propertyName,
        out Type resultingElementType)
    {
        PropertyInfo propertyInfo = elementType.GetProperty(propertyName)
                                    ?? throw new FilterException(
                                        $"Property '{propertyName}' is not defined on type '{elementType.Name}'.");

        ParameterExpression itemParameter = Expression.Parameter(elementType, "item");
        MemberExpression elementPropertyAccess = Expression.Property(itemParameter, propertyInfo);
        LambdaExpression selectorLambda = Expression.Lambda(elementPropertyAccess, itemParameter);
        Type propertyType = propertyInfo.PropertyType;

        Type? nestedEnumerableInterface = FindGenericEnumerableInterface(propertyType);
        if (nestedEnumerableInterface != null && propertyType != typeof(string))
        {
            resultingElementType = nestedEnumerableInterface.GetGenericArguments()[0];
            return Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.SelectMany),
                [elementType, resultingElementType],
                sequenceExpression,
                selectorLambda);
        }

        resultingElementType = propertyType;
        return Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Select),
            [elementType, resultingElementType],
            sequenceExpression,
            selectorLambda);
    }

    /// <summary>
    /// Yields every reachable dot-path from the root type using BFS.
    /// </summary>
    private static IEnumerable<PathWithTypesRecord> GetAvailablePaths(Type rootType)
    {
        HashSet<PropertyInfo> visitedProps = [];
        Queue<PathNode> levelQueue = [];

        levelQueue.Enqueue(new PathNode(rootType, string.Empty, skipVisitedGuard: true));

        while (levelQueue.Count > 0)
        {
            List<PathNode> thisLevel = [];
            while (levelQueue.Count > 0)
                thisLevel.Add(levelQueue.Dequeue());

            foreach (PathNode node in thisLevel)
            {
                foreach (PropertyInfo prop in node.ClrType
                             .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.CanRead && p.CanWrite)
                             .Where(p => p.PropertyType.IsValueType || p.PropertyType == typeof(string)))
                {
                    if (!node.SkipVisitedGuard && !visitedProps.Add(prop))
                        continue;

                    string fullPath = string.IsNullOrEmpty(node.Path)
                        ? prop.Name
                        : $"{node.Path}{FilterConstants.PATH_SEPARATOR}{prop.Name}";

                    yield return new PathWithTypesRecord(fullPath, []);
                }
            }

            List<PathNode> nextLevel = [];

            foreach (PathNode node in thisLevel)
            {
                foreach (PropertyInfo prop in node.ClrType
                             .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.CanRead && p.CanWrite)
                             .Where(p => !(p.PropertyType.IsValueType || p.PropertyType == typeof(string))))
                {
                    if (!node.SkipVisitedGuard && !visitedProps.Add(prop))
                        continue;

                    string childPath = string.IsNullOrEmpty(node.Path)
                        ? prop.Name
                        : $"{node.Path}{FilterConstants.PATH_SEPARATOR}{prop.Name}";

                    Type? enumerableIface = FindGenericEnumerableInterface(prop.PropertyType);
                    bool isCollection = enumerableIface is not null && prop.PropertyType != typeof(string);
                    Type elementType = isCollection
                        ? enumerableIface!.GetGenericArguments()[0]
                        : prop.PropertyType;

                    yield return new PathWithTypesRecord(
                        childPath,
                        isCollection ? [elementType] : []);

                    nextLevel.Add(new PathNode(
                        elementType,
                        childPath,
                        skipVisitedGuard: false));
                }
            }

            foreach (PathNode n in nextLevel)
                levelQueue.Enqueue(n);
        }
    }
}
