using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a physical inventory count session scoped to a warehouse and optional zone.
/// <para>See <see cref="StocktakeCount"/>, <see cref="WarehouseEntity"/>, <see cref="Zone"/>.</para>
/// </summary>
[Table("StocktakeSessions", Schema = "inventory")]
public sealed class StocktakeSession : IEntity
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
    /// Gets or sets the session name (max 200 characters).
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets optional notes (max 2000 characters).
    /// </summary>
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the session status (Draft, InProgress, Completed, Cancelled).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this session.
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the session was started.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the session was completed.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who completed this session.
    /// </summary>
    public int? CompletedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the warehouse.
    /// </summary>
    public WarehouseEntity Warehouse { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the zone.
    /// </summary>
    public Zone? Zone { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of count entries.
    /// </summary>
    public ICollection<StocktakeCount> Counts { get; set; } = [];
}
