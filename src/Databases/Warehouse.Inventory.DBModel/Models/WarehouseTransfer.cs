using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents a stock transfer between two warehouses with line items.
/// <para>See <see cref="WarehouseTransferLine"/>, <see cref="WarehouseEntity"/>.</para>
/// </summary>
[Table("WarehouseTransfers", Schema = "inventory")]
public sealed class WarehouseTransfer : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the source warehouse.
    /// </summary>
    [ForeignKey(nameof(SourceWarehouse))]
    public int SourceWarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the destination warehouse.
    /// </summary>
    [ForeignKey(nameof(DestinationWarehouse))]
    public int DestinationWarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the transfer status (Draft, Completed, Cancelled).
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public required string Status { get; set; }

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
    /// Gets or sets the ID of the user who created this transfer.
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the transfer was completed.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who completed this transfer.
    /// </summary>
    public int? CompletedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the source warehouse.
    /// </summary>
    public WarehouseEntity SourceWarehouse { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the destination warehouse.
    /// </summary>
    public WarehouseEntity DestinationWarehouse { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation collection of transfer lines.
    /// </summary>
    public ICollection<WarehouseTransferLine> Lines { get; set; } = [];
}
