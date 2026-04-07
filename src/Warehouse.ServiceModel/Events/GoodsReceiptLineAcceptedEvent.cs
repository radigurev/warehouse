namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published when a quarantined goods receipt line is resolved to Accepted.
/// Event naming: Purchasing.GoodsReceiptLine.Accepted.
/// </summary>
public sealed record GoodsReceiptLineAcceptedEvent
{
    /// <summary>
    /// Gets the goods receipt ID.
    /// </summary>
    public required int GoodsReceiptId { get; init; }

    /// <summary>
    /// Gets the goods receipt line ID.
    /// </summary>
    public required int GoodsReceiptLineId { get; init; }

    /// <summary>
    /// Gets the product ID.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the warehouse ID.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the optional location ID.
    /// </summary>
    public int? LocationId { get; init; }

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

    /// <summary>
    /// Gets the user who accepted the line.
    /// </summary>
    public required int AcceptedByUserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the line was accepted.
    /// </summary>
    public required DateTime AcceptedAtUtc { get; init; }
}
