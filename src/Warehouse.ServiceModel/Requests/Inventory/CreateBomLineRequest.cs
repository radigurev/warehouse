namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for a single BOM component line.
/// </summary>
public sealed record CreateBomLineRequest
{
    /// <summary>
    /// Gets the child product ID. Required.
    /// </summary>
    public required int ChildProductId { get; init; }

    /// <summary>
    /// Gets the required quantity. Must be greater than zero.
    /// </summary>
    public required decimal Quantity { get; init; }
}
