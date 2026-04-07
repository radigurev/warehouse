using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a line item on a purchase order referencing a product from the Inventory domain.
/// <para>See <see cref="PurchaseOrder"/>.</para>
/// </summary>
[Table("PurchaseOrderLines", Schema = "purchasing")]
[Index(nameof(PurchaseOrderId), Name = "IX_PurchaseOrderLines_PurchaseOrderId")]
[Index(nameof(ProductId), Name = "IX_PurchaseOrderLines_ProductId")]
[Index(nameof(PurchaseOrderId), nameof(ProductId), IsUnique = true, Name = "IX_PurchaseOrderLines_POId_ProductId")]
public sealed class PurchaseOrderLine : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the purchase order.
    /// </summary>
    [Required]
    [ForeignKey(nameof(PurchaseOrder))]
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// Gets or sets the product ID (cross-schema ref to inventory.Products).
    /// </summary>
    [Required]
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the ordered quantity.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal OrderedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the line total (OrderedQuantity * UnitPrice).
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Gets or sets the total received quantity across all goods receipts.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal ReceivedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 500 characters).
    /// </summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the purchase order.
    /// </summary>
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation collection of goods receipt lines for this PO line.
    /// </summary>
    public ICollection<GoodsReceiptLine> GoodsReceiptLines { get; set; } = [];
}
