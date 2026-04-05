namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for a single adjustment line within an adjustment.
/// </summary>
public sealed record CreateAdjustmentLineRequest
{
    /// <summary>
    /// Gets the product ID. Required.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the optional location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the actual counted quantity. Required.
    /// </summary>
    public required decimal ActualQuantity { get; init; }
}
