namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for creating a new purchase order with lines.
/// </summary>
public sealed record CreatePurchaseOrderRequest
{
    /// <summary>
    /// Gets the supplier ID. Required.
    /// </summary>
    public required int SupplierId { get; init; }

    /// <summary>
    /// Gets the destination warehouse ID. Required.
    /// </summary>
    public required int DestinationWarehouseId { get; init; }

    /// <summary>
    /// Gets the expected delivery date. Optional, must be today or future.
    /// </summary>
    public DateOnly? ExpectedDeliveryDate { get; init; }

    /// <summary>
    /// Gets the PO notes. Optional, max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets the collection of PO lines. At least one required.
    /// </summary>
    public required IReadOnlyList<CreatePurchaseOrderLineRequest> Lines { get; init; }
}
