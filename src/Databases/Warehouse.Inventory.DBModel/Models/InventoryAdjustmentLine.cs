using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a single line within an inventory adjustment specifying expected and actual quantities.
/// <para>See <see cref="InventoryAdjustment"/>.</para>
/// </summary>
[Table("InventoryAdjustmentLines", Schema = "inventory")]
public sealed class InventoryAdjustmentLine : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the parent adjustment.
    /// </summary>
    [ForeignKey(nameof(Adjustment))]
    public int AdjustmentId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the product.
    /// </summary>
    [ForeignKey(nameof(Product))]
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the warehouse.
    /// </summary>
    [ForeignKey(nameof(Warehouse))]
    public int WarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the optional foreign key to the storage location.
    /// </summary>
    [ForeignKey(nameof(Location))]
    public int? LocationId { get; set; }

    /// <summary>
    /// Gets or sets the optional foreign key to the batch.
    /// </summary>
    [ForeignKey(nameof(Batch))]
    public int? BatchId { get; set; }

    /// <summary>
    /// Gets or sets the expected quantity (from system records).
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal ExpectedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the actual quantity (from physical count).
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal ActualQuantity { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent adjustment.
    /// </summary>
    public InventoryAdjustment Adjustment { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the product.
    /// </summary>
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the warehouse.
    /// </summary>
    public WarehouseEntity Warehouse { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the storage location.
    /// </summary>
    public StorageLocation? Location { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the batch.
    /// </summary>
    public Batch? Batch { get; set; }
}
