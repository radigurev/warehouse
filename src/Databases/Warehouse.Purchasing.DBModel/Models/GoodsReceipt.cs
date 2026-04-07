using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a goods receipt document recording material received against a purchase order.
/// <para>Conforms to ISA-95 Part 3 -- Material Receipt activity.</para>
/// <para>See <see cref="PurchaseOrder"/>, <see cref="GoodsReceiptLine"/>.</para>
/// </summary>
[Table("GoodsReceipts", Schema = "purchasing")]
[Index(nameof(ReceiptNumber), IsUnique = true, Name = "IX_GoodsReceipts_ReceiptNumber")]
[Index(nameof(PurchaseOrderId), Name = "IX_GoodsReceipts_PurchaseOrderId")]
[Index(nameof(WarehouseId), Name = "IX_GoodsReceipts_WarehouseId")]
[Index(nameof(ReceivedAtUtc), Name = "IX_GoodsReceipts_ReceivedAtUtc")]
public sealed class GoodsReceipt : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique receipt number (format: GR-YYYYMMDD-NNNN).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string ReceiptNumber { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the purchase order.
    /// </summary>
    [Required]
    [ForeignKey(nameof(PurchaseOrder))]
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// Gets or sets the receiving warehouse ID (cross-schema ref to inventory.Warehouses).
    /// </summary>
    [Required]
    public int WarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the optional receiving location ID (cross-schema ref to inventory.StorageLocations).
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Gets or sets the receipt status (stored as nvarchar(20)).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the goods were received.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime ReceivedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this receipt (cross-schema ref to auth.Users).
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the receipt was completed.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
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
