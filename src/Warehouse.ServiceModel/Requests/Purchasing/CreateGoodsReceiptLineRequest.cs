namespace Warehouse.ServiceModel.Requests.Purchasing;

/// <summary>
/// Request payload for a goods receipt line.
/// </summary>
public sealed record CreateGoodsReceiptLineRequest
{
    /// <summary>
    /// Gets the purchase order line ID. Required.
    /// </summary>
    public required int PurchaseOrderLineId { get; init; }

    /// <summary>
    /// Gets the received quantity. Required, must be greater than 0.
    /// </summary>
    public required decimal ReceivedQuantity { get; init; }

    /// <summary>
    /// Gets the batch number. Optional, max 50 characters.
    /// </summary>
    public string? BatchNumber { get; init; }

    /// <summary>
    /// Gets the manufacturing date. Optional, must not be future.
    /// </summary>
    public DateOnly? ManufacturingDate { get; init; }

    /// <summary>
    /// Gets the expiry date. Optional, must be after manufacturing date.
    /// </summary>
    public DateOnly? ExpiryDate { get; init; }
}
