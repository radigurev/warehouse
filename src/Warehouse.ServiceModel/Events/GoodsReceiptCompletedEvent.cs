namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published when a goods receipt is completed with at least one accepted line.
/// Event naming: Purchasing.GoodsReceipt.Completed.
/// </summary>
public sealed record GoodsReceiptCompletedEvent : ICorrelatedEvent
{
    /// <summary>
    /// Gets the goods receipt ID.
    /// </summary>
    public required int GoodsReceiptId { get; init; }

    /// <summary>
    /// Gets the goods receipt number.
    /// </summary>
    public required string GoodsReceiptNumber { get; init; }

    /// <summary>
    /// Gets the purchase order ID.
    /// </summary>
    public required int PurchaseOrderId { get; init; }

    /// <summary>
    /// Gets the purchase order number.
    /// </summary>
    public required string PurchaseOrderNumber { get; init; }

    /// <summary>
    /// Gets the warehouse ID where goods were received.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the optional location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the user who completed the receipt.
    /// </summary>
    public required int ReceivedByUserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the receipt was completed.
    /// </summary>
    public required DateTime ReceivedAtUtc { get; init; }

    /// <summary>
    /// Gets the collection of accepted receipt lines.
    /// </summary>
    public required IReadOnlyList<GoodsReceiptCompletedLine> Lines { get; init; }

    /// <summary>
    /// Gets the correlation ID from the originating HTTP request.
    /// </summary>
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Represents a single accepted line in a goods receipt completed event.
/// </summary>
public sealed record GoodsReceiptCompletedLine
{
    /// <summary>
    /// Gets the goods receipt line ID.
    /// </summary>
    public required int GoodsReceiptLineId { get; init; }

    /// <summary>
    /// Gets the product ID.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the accepted quantity.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Gets the batch number.
    /// </summary>
    public string? BatchNumber { get; init; }

    /// <summary>
    /// Gets the manufacturing date.
    /// </summary>
    public DateOnly? ManufacturingDate { get; init; }

    /// <summary>
    /// Gets the expiry date.
    /// </summary>
    public DateOnly? ExpiryDate { get; init; }
}
