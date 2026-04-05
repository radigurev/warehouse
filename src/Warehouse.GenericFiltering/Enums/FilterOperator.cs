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
