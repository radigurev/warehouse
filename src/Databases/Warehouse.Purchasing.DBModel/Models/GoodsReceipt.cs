using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a goods receipt document recording material received against a purchase order.
/// <para>Conforms to ISA-95 Part 3 -- Material Receipt activity.</para>
/// <para>See <see cref="PurchaseOrder"/>, <see cref="GoodsReceiptLine"/>.</para>
/// </summary>
public sealed class GoodsReceipt : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique receipt number (format: GR-YYYYMMDD-NNNN).
    /// </summary>
    public required string ReceiptNumber { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the purchase order.
    /// </summary>
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// Gets or sets the receiving warehouse ID (cross-schema ref to inventory.Warehouses).
    /// </summary>
    public int WarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the optional receiving location ID (cross-schema ref to inventory.StorageLocations).
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Gets or sets the receipt status (stored as nvarchar(20)).
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the goods were received.
    /// </summary>
    public DateTime ReceivedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this receipt (cross-schema ref to auth.Users).
    /// </summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the receipt was completed.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the purchase order.
    /// </summary>
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation collection of receipt lines.
    /// </summary>
    public ICollection<GoodsReceiptLine> Lines { get; set; } = [];
}
