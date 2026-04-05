using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a zone within a warehouse for organizing storage locations.
/// <para>See <see cref="WarehouseEntity"/>, <see cref="StorageLocation"/>.</para>
/// </summary>
[Table("Zones", Schema = "inventory")]
public sealed class Zone : IEntity
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
    /// Gets or sets the zone code (max 20 characters), unique within warehouse.
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the zone name (max 100 characters).
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the optional description (max 500 characters).
    /// </summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? Description { get; set; }

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
    /// Gets or sets the navigation collection of storage locations.
    /// </summary>
    public ICollection<StorageLocation> Locations { get; set; } = [];
}
