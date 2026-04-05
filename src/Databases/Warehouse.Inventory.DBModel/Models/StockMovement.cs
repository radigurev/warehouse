using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents an immutable record of a stock quantity change with reason and reference.
/// <para>See <see cref="Product"/>, <see cref="WarehouseEntity"/>, <see cref="StorageLocation"/>.</para>
/// </summary>
[Table("StockMovements", Schema = "inventory")]
public sealed class StockMovement : IEntity
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
    /// Gets or sets the movement quantity (positive for inbound, negative for outbound).
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the reason code for the movement.
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public required string ReasonCode { get; set; }

    /// <summary>
    /// Gets or sets the optional reference type (e.g., Order, Adjustment, Transfer).
    /// </summary>
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public string? ReferenceType { get; set; }

    /// <summary>
    /// Gets or sets the optional reference entity ID.
    /// </summary>
    public int? ReferenceId { get; set; }

    /// <summary>
    /// Gets or sets the optional reference number (max 100 characters).
    /// </summary>
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Gets or sets optional notes (max 2000 characters).
    /// </summary>
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this movement.
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }

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
