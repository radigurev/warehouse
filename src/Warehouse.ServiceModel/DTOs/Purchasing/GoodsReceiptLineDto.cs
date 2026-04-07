namespace Warehouse.ServiceModel.DTOs.Purchasing;

/// <summary>
/// Represents a goods receipt line with quantity and inspection status.
/// </summary>
public sealed record GoodsReceiptLineDto
{
    /// <summary>
    /// Gets the line ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the purchase order line ID.
    /// </summary>
    public required int PurchaseOrderLineId { get; init; }

    /// <summary>
    /// Gets the received quantity.
    /// </summary>
    public required decimal ReceivedQuantity { get; init; }

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
    /// Gets the inspection status.
    /// </summary>
    public required string InspectionStatus { get; init; }

    /// <summary>
    /// Gets the inspection note.
    /// </summary>
    public string? InspectionNote { get; init; }

    /// <summary>
    /// Gets the UTC inspection timestamp.
    /// </summary>
    public DateTime? InspectedAtUtc { get; init; }
}
