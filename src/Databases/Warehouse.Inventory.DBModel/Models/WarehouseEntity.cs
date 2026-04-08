using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a physical warehouse with zones and storage locations.
/// Named WarehouseEntity to avoid conflict with the Warehouse namespace.
/// <para>See <see cref="Zone"/>, <see cref="StorageLocation"/>.</para>
/// </summary>
[Table("Warehouses", Schema = "inventory")]
[Index(nameof(Code), IsUnique = true, Name = "IX_Warehouses_Code")]
public sealed class WarehouseEntity : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique warehouse code (max 20 characters).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the warehouse name (max 200 characters).
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the optional warehouse address (max 500 characters).
    /// </summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets whether the warehouse is soft-deleted.
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the warehouse was soft-deleted.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this warehouse.
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional ID of the user who last modified this warehouse.
    /// </summary>
    public int? ModifiedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of zones.
    /// </summary>
    public ICollection<Zone> Zones { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of storage locations.
    /// </summary>
    public ICollection<StorageLocation> Locations { get; set; } = [];
}
