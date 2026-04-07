namespace Warehouse.ServiceModel.DTOs.Purchasing;

/// <summary>
/// Full goods receipt representation including lines and inspection status.
/// </summary>
public sealed record GoodsReceiptDetailDto
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
    /// Gets the optional location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the receipt status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the optional notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets the UTC received timestamp.
    /// </summary>
    public required DateTime ReceivedAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC completed timestamp.
    /// </summary>
    public DateTime? CompletedAtUtc { get; init; }

    /// <summary>
    /// Gets the collection of receipt lines.
    /// </summary>
    public required IReadOnlyList<GoodsReceiptLineDto> Lines { get; init; }
}
