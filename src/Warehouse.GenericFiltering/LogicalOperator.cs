namespace Warehouse.GenericFiltering;

/// <summary>
/// Logical operators for combining filter expressions.
/// </summary>
public enum LogicalOperator
{
    /// <summary>
    /// Logical AND — both conditions must be true.
    /// </summary>
    And,

    /// <summary>
    /// Logical OR — at least one condition must be true.
    /// </summary>
    Or
}
