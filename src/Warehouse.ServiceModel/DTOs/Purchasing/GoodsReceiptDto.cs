namespace Warehouse.ServiceModel.DTOs.Purchasing;

/// <summary>
/// Lightweight goods receipt representation for list views.
/// </summary>
public sealed record GoodsReceiptDto
{
    /// <summary>
    /// Gets the receipt ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the unique receipt number.
    /// </summary>
    public required string ReceiptNumber { get; init; }

    /// <summary>
    /// Gets the purchase order ID.
    /// </summary>
    public required int PurchaseOrderId { get; init; }

    /// <summary>
    /// Gets the PO number.
    /// </summary>
    public required string PurchaseOrderNumber { get; init; }

    /// <summary>
    /// Gets the warehouse ID.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the receipt status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the UTC received timestamp.
    /// </summary>
    public required DateTime ReceivedAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC completed timestamp.
    /// </summary>
    public DateTime? CompletedAtUtc { get; init; }
}
