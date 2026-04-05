using System.Linq.Expressions;

namespace Warehouse.GenericFiltering;

/// <summary>
/// Orchestrates filter expression building by combining path resolution, value conversion, and comparison logic.
/// <para>Produces a complete <see cref="Expression{TDelegate}"/> predicate from a <see cref="FilterGroup"/>.</para>
/// </summary>
public static class FilterExpressionBuilder
{
    /// <summary>
    /// Builds a composite predicate expression from all descriptors in the filter group.
    /// </summary>
    public static Expression<Func<T, bool>> Build<T>(FilterGroup filterGroup) where T : class
    {
        IReadOnlyList<FilterDescriptor> descriptors = filterGroup.Descriptors;

        if (descriptors.Count == 0)
            throw new FilterException("FilterGroup contains no descriptors.");

        Expression<Func<T, bool>> result = BuildSinglePredicate<T>(descriptors[0]);

        for (int i = 1; i < descriptors.Count; i++)
        {
            Expression<Func<T, bool>> next = BuildSinglePredicate<T>(descriptors[i]);
            LogicalOperator? logical = descriptors[i - 1].NextLogical;

            result = logical switch
            {
                LogicalOperator.And => CombineAnd(result, next),
                LogicalOperator.Or => CombineOr(result, next),
                _ => result
            };
        }

        return result;
    }

    /// <summary>
    /// Builds a single predicate expression from one filter descriptor.
    /// </summary>
    private static Expression<Func<T, bool>> BuildSinglePredicate<T>(FilterDescriptor descriptor) where T : class
    {
        System.Collections.Concurrent.ConcurrentDictionary<string, LambdaExpression> selectors =
            PropertyPathResolver.GetSelectors(typeof(T));

        if (!selectors.TryGetValue(descriptor.PropertyPath.ToLowerInvariant(), out LambdaExpression? selector))
            throw new FilterException(
                $"Path '{descriptor.PropertyPath}' is not available for type '{typeof(T).Name}'.");

        ParameterExpression parameter = Expression.Parameter(typeof(T), FilterConstants.PARAMETER_NAME);
        ParameterReplacer replacer = new(selector.Parameters[0], parameter);
        Expression visitorResult = replacer.Visit(selector.Body);

        Expression constant = ValueConverter.ConvertToExpression(selector, descriptor.RawValue);
        Expression body = ComparisonBuilder.BuildComparison(visitorResult, constant, descriptor.Operator);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    /// <summary>
    /// Combines two predicates with a logical AND (short-circuit).
    /// </summary>
    private static Expression<Func<T, bool>> CombineAnd<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right) where T : class
    {
        ParameterExpression parameter = left.Parameters[0];
        ParameterReplacer rightBody = new(right.Parameters[0], parameter);
        Expression body = Expression.AndAlso(left.Body, rightBody.Visit(right.Body));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    /// <summary>
    /// Combines two predicates with a logical OR (short-circuit).
    /// </summary>
    private static Expression<Func<T, bool>> CombineOr<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right) where T : class
    {
        ParameterExpression parameter = left.Parameters[0];
        ParameterReplacer rightBody = new(right.Parameters[0], parameter);
        Expression body = Expression.OrElse(left.Body, rightBody.Visit(right.Body));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
