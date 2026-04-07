using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a purchase order header with supplier reference and status lifecycle.
/// <para>Conforms to ISA-95 Part 3 -- Procurement Operations Activity Model.</para>
/// <para>See <see cref="Supplier"/>, <see cref="PurchaseOrderLine"/>.</para>
/// </summary>
public sealed class PurchaseOrder : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique PO number (format: PO-YYYYMMDD-NNNN).
    /// </summary>
    public required string OrderNumber { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the supplier.
    /// </summary>
    public int SupplierId { get; set; }

    /// <summary>
    /// Gets or sets the PO status (stored as nvarchar(30)).
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the destination warehouse ID (cross-schema ref to inventory.Warehouses).
    /// </summary>
    public int DestinationWarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the expected delivery date.
    /// </summary>
    public DateOnly? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the total amount (computed from lines).
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this PO (cross-schema ref to auth.Users).
    /// </summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional ID of the user who last modified this PO.
    /// </summary>
    public int? ModifiedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the PO was confirmed.
    /// </summary>
    public DateTime? ConfirmedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who confirmed this PO.
    /// </summary>
    public int? ConfirmedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the PO was closed.
    /// </summary>
    public DateTime? ClosedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who closed this PO.
    /// </summary>
    public int? ClosedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the supplier.
    /// </summary>
    public Supplier Supplier { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation collection of PO lines.
    /// </summary>
    public ICollection<PurchaseOrderLine> Lines { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of goods receipts for this PO.
    /// </summary>
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = [];
}
