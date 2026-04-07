namespace Warehouse.ServiceModel.DTOs.Purchasing;

/// <summary>
/// Lightweight purchase order representation for list views.
/// </summary>
public sealed record PurchaseOrderDto
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
    /// Gets the total amount.
    /// </summary>
    public required decimal TotalAmount { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
