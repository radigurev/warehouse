using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a single line on a goods receipt, recording received quantity and inspection status.
/// <para>See <see cref="GoodsReceipt"/>, <see cref="PurchaseOrderLine"/>.</para>
/// </summary>
public sealed class GoodsReceiptLine : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the goods receipt.
    /// </summary>
    public int GoodsReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the purchase order line.
    /// </summary>
    public int PurchaseOrderLineId { get; set; }

    /// <summary>
    /// Gets or sets the received quantity.
    /// </summary>
    public decimal ReceivedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the optional batch number (max 50 characters).
    /// </summary>
    public string? BatchNumber { get; set; }

    /// <summary>
    /// Gets or sets the optional manufacturing date.
    /// </summary>
    public DateOnly? ManufacturingDate { get; set; }

    /// <summary>
    /// Gets or sets the optional expiry date.
    /// </summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets the inspection status (stored as nvarchar(20)).
    /// </summary>
    public required string InspectionStatus { get; set; }

    /// <summary>
    /// Gets or sets the optional inspection note (max 2000 characters).
    /// </summary>
    public string? InspectionNote { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the line was inspected.
    /// </summary>
    public DateTime? InspectedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who inspected this line (cross-schema ref to auth.Users).
    /// </summary>
    public int? InspectedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the goods receipt.
    /// </summary>
    public GoodsReceipt GoodsReceipt { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the purchase order line.
    /// </summary>
    public PurchaseOrderLine PurchaseOrderLine { get; set; } = null!;
}
