using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a single line on a supplier return, specifying the product and quantity to return.
/// <para>See <see cref="SupplierReturn"/>.</para>
/// </summary>
[Table("SupplierReturnLines", Schema = "purchasing")]
[Index(nameof(SupplierReturnId), Name = "IX_SupplierReturnLines_SupplierReturnId")]
public sealed class SupplierReturnLine : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the supplier return.
    /// </summary>
    [Required]
    [ForeignKey(nameof(SupplierReturn))]
    public int SupplierReturnId { get; set; }

    /// <summary>
    /// Gets or sets the product ID (cross-schema ref to inventory.Products).
    /// </summary>
    [Required]
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the warehouse ID (cross-schema ref to inventory.Warehouses).
    /// </summary>
    [Required]
    public int WarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the optional location ID (cross-schema ref to inventory.StorageLocations).
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Gets or sets the return quantity.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the optional batch ID (cross-schema ref to inventory.Batches).
    /// </summary>
    public int? BatchId { get; set; }

    /// <summary>
    /// Gets or sets the optional reference to the original goods receipt line.
    /// </summary>
    [ForeignKey(nameof(GoodsReceiptLine))]
    public int? GoodsReceiptLineId { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 500 characters).
    /// </summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the supplier return.
    /// </summary>
    public SupplierReturn SupplierReturn { get; set; } = null!;

    /// <summary>
    /// Gets or sets the optional navigation property to the goods receipt line.
    /// </summary>
    public GoodsReceiptLine? GoodsReceiptLine { get; set; }
}
