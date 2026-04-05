using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents current stock quantities for a product at a specific warehouse and location.
/// <para>See <see cref="Product"/>, <see cref="WarehouseEntity"/>, <see cref="StorageLocation"/>.</para>
/// </summary>
[Table("StockLevels", Schema = "inventory")]
public sealed class StockLevel : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

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
    /// Gets or sets the quantity currently on hand.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityOnHand { get; set; }

    /// <summary>
    /// Gets or sets the quantity reserved for orders.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityReserved { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? ModifiedAtUtc { get; set; }

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
