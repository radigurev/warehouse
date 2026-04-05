namespace Warehouse.GenericFiltering;

/// <summary>
/// Comparison operators supported by the filter engine.
/// </summary>
public enum FilterOperator
{
    /// <summary>
    /// Exact equality comparison.
    /// </summary>
    Equals,

    /// <summary>
    /// Inequality comparison.
    /// </summary>
    NotEquals,

    /// <summary>
    /// Strictly greater than comparison.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater than or equal comparison.
    /// </summary>
    GreaterOrEqual,

    /// <summary>
    /// Strictly less than comparison.
    /// </summary>
    LessThan,

    /// <summary>
    /// Less than or equal comparison.
    /// </summary>
    LessOrEqual,

    /// <summary>
    /// Substring containment comparison.
    /// </summary>
    Contains,

    /// <summary>
    /// Negated substring containment comparison.
    /// </summary>
    NotContains
}

/// <summary>
/// Provides parsing of string operator tokens to <see cref="FilterOperator"/> values.
/// </summary>
public static class FilterOperatorParser
{
    /// <summary>
    /// Parses a string token (e.g. "eq", "cn") to the corresponding <see cref="FilterOperator"/>.
    /// </summary>
    public static FilterOperator Parse(string token) => token.ToLowerInvariant() switch
    {
        "eq" => FilterOperator.Equals,
        "nq" => FilterOperator.NotEquals,
        "gt" => FilterOperator.GreaterThan,
        "ge" => FilterOperator.GreaterOrEqual,
        "lt" => FilterOperator.LessThan,
        "le" => FilterOperator.LessOrEqual,
        "cn" => FilterOperator.Contains,
        "ncn" => FilterOperator.NotContains,
        _ => throw new FilterException($"Unknown filter operator: '{token}'.")
    };
}
