using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a storage location within a warehouse zone (row, shelf, bin, or bulk).
/// <para>See <see cref="WarehouseEntity"/>, <see cref="Zone"/>.</para>
/// </summary>
[Table("StorageLocations", Schema = "inventory")]
public sealed class StorageLocation : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the warehouse.
    /// </summary>
    [ForeignKey(nameof(Warehouse))]
    public int WarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the optional foreign key to the zone.
    /// </summary>
    [ForeignKey(nameof(Zone))]
    public int? ZoneId { get; set; }

    /// <summary>
    /// Gets or sets the location code (max 30 characters), unique within warehouse.
    /// </summary>
    [Required]
    [MaxLength(30)]
    [Column(TypeName = "nvarchar(30)")]
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the location name (max 100 characters).
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the location type (Row, Shelf, Bin, Bulk).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string LocationType { get; set; }

    /// <summary>
    /// Gets or sets the optional capacity of the location.
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? Capacity { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the warehouse.
    /// </summary>
    public WarehouseEntity Warehouse { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the zone.
    /// </summary>
    public Zone? Zone { get; set; }
}
