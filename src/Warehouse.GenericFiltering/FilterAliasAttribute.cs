namespace Warehouse.GenericFiltering;

/// <summary>
/// Specifies an alias name for a filterable property path.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class FilterAliasAttribute : Attribute
{
    /// <summary>
    /// Gets the alias name used in filter expressions.
    /// </summary>
    public string Alias { get; }

    /// <summary>
    /// Gets the optional dot-separated path that scopes this alias.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Initializes a new instance with the specified alias and optional path scope.
    /// </summary>
    public FilterAliasAttribute(string alias, string path = "")
    {
        Alias = alias;
        Path = path;
    }
}
