using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a single product line within a warehouse transfer.
/// <para>See <see cref="WarehouseTransfer"/>, <see cref="Product"/>.</para>
/// </summary>
[Table("WarehouseTransferLines", Schema = "inventory")]
public sealed class WarehouseTransferLine : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the parent transfer.
    /// </summary>
    [ForeignKey(nameof(Transfer))]
    public int TransferId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the product being transferred.
    /// </summary>
    [ForeignKey(nameof(Product))]
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the transfer quantity.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the optional source location within the source warehouse.
    /// </summary>
    [ForeignKey(nameof(SourceLocation))]
    public int? SourceLocationId { get; set; }

    /// <summary>
    /// Gets or sets the optional destination location within the destination warehouse.
    /// </summary>
    [ForeignKey(nameof(DestinationLocation))]
    public int? DestinationLocationId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent transfer.
    /// </summary>
    public WarehouseTransfer Transfer { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the product.
    /// </summary>
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the source location.
    /// </summary>
    public StorageLocation? SourceLocation { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the destination location.
    /// </summary>
    public StorageLocation? DestinationLocation { get; set; }
}
