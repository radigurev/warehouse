using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a single line on a goods receipt, recording received quantity and inspection status.
/// <para>See <see cref="GoodsReceipt"/>, <see cref="PurchaseOrderLine"/>.</para>
/// </summary>
[Table("GoodsReceiptLines", Schema = "purchasing")]
[Index(nameof(GoodsReceiptId), Name = "IX_GoodsReceiptLines_GoodsReceiptId")]
[Index(nameof(PurchaseOrderLineId), Name = "IX_GoodsReceiptLines_PurchaseOrderLineId")]
public sealed class GoodsReceiptLine : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the goods receipt.
    /// </summary>
    [Required]
    [ForeignKey(nameof(GoodsReceipt))]
    public int GoodsReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the purchase order line.
    /// </summary>
    [Required]
    [ForeignKey(nameof(PurchaseOrderLine))]
    public int PurchaseOrderLineId { get; set; }

    /// <summary>
    /// Gets or sets the received quantity.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal ReceivedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the optional batch number (max 50 characters).
    /// </summary>
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public string? BatchNumber { get; set; }

    /// <summary>
    /// Gets or sets the optional manufacturing date.
    /// </summary>
    [Column(TypeName = "date")]
    public DateOnly? ManufacturingDate { get; set; }

    /// <summary>
    /// Gets or sets the optional expiry date.
    /// </summary>
    [Column(TypeName = "date")]
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets the inspection status (stored as nvarchar(20)).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string InspectionStatus { get; set; }

    /// <summary>
    /// Gets or sets the optional inspection note (max 2000 characters).
    /// </summary>
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string? InspectionNote { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the line was inspected.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
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
