namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for updating a purchase order header.
/// </summary>
public sealed record UpdatePurchaseOrderRequest
{
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
}
