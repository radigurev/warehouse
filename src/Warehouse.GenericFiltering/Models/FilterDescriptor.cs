namespace Warehouse.GenericFiltering;

/// <summary>
/// Immutable representation of a single filter clause.
/// </summary>
public sealed record FilterDescriptor
{
    /// <summary>
    /// Gets the dot-separated property path (e.g. "customer.name").
    /// </summary>
    public required string PropertyPath { get; init; }

    /// <summary>
    /// Gets the comparison operator.
    /// </summary>
    public required FilterOperator Operator { get; init; }

    /// <summary>
    /// Gets the raw string value to compare against.
    /// </summary>
    public required string RawValue { get; init; }

    /// <summary>
    /// Gets the logical operator connecting this descriptor to the next, if any.
    /// </summary>
    public LogicalOperator? NextLogical { get; init; }
}
