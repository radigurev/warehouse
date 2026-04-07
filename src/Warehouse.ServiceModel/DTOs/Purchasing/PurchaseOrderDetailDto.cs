namespace Warehouse.ServiceModel.DTOs.Purchasing;

/// <summary>
/// Full purchase order representation including lines and receiving progress.
/// </summary>
public sealed record PurchaseOrderDetailDto
{
    /// <summary>
    /// Gets the purchase order ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the unique PO number.
    /// </summary>
    public required string OrderNumber { get; init; }

    /// <summary>
    /// Gets the supplier ID.
    /// </summary>
    public required int SupplierId { get; init; }

    /// <summary>
    /// Gets the supplier name.
    /// </summary>
    public required string SupplierName { get; init; }

    /// <summary>
    /// Gets the current status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the destination warehouse ID.
    /// </summary>
    public required int DestinationWarehouseId { get; init; }

    /// <summary>
    /// Gets the expected delivery date.
    /// </summary>
    public DateOnly? ExpectedDeliveryDate { get; init; }

    /// <summary>
    /// Gets the optional notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets the total amount.
    /// </summary>
    public required decimal TotalAmount { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC confirmation timestamp.
    /// </summary>
    public DateTime? ConfirmedAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC closing timestamp.
    /// </summary>
    public DateTime? ClosedAtUtc { get; init; }

    /// <summary>
    /// Gets the collection of PO lines with receiving progress.
    /// </summary>
    public required IReadOnlyList<PurchaseOrderLineDto> Lines { get; init; }
}
