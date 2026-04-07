using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a purchase order header with supplier reference and status lifecycle.
/// <para>Conforms to ISA-95 Part 3 -- Procurement Operations Activity Model.</para>
/// <para>See <see cref="Supplier"/>, <see cref="PurchaseOrderLine"/>.</para>
/// </summary>
[Table("PurchaseOrders", Schema = "purchasing")]
[Index(nameof(OrderNumber), IsUnique = true, Name = "IX_PurchaseOrders_OrderNumber")]
[Index(nameof(SupplierId), Name = "IX_PurchaseOrders_SupplierId")]
[Index(nameof(Status), Name = "IX_PurchaseOrders_Status")]
[Index(nameof(CreatedAtUtc), Name = "IX_PurchaseOrders_CreatedAtUtc")]
[Index(nameof(DestinationWarehouseId), Name = "IX_PurchaseOrders_DestinationWarehouseId")]
public sealed class PurchaseOrder : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique PO number (format: PO-YYYYMMDD-NNNN).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string OrderNumber { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the supplier.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Supplier))]
    public int SupplierId { get; set; }

    /// <summary>
    /// Gets or sets the PO status (stored as nvarchar(30)).
    /// </summary>
    [Required]
    [MaxLength(30)]
    [Column(TypeName = "nvarchar(30)")]
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the destination warehouse ID (cross-schema ref to inventory.Warehouses).
    /// </summary>
    [Required]
    public int DestinationWarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the expected delivery date.
    /// </summary>
    [Column(TypeName = "date")]
    public DateOnly? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the total amount (computed from lines).
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this PO (cross-schema ref to auth.Users).
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional ID of the user who last modified this PO.
    /// </summary>
    public int? ModifiedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the PO was confirmed.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? ConfirmedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who confirmed this PO.
    /// </summary>
    public int? ConfirmedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the PO was closed.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
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
