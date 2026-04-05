namespace Warehouse.GenericFiltering;

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
        "sw" => FilterOperator.StartsWith,
        "ew" => FilterOperator.EndsWith,
        _ => throw new FilterException($"Unknown filter operator: '{token}'.")
    };
}
