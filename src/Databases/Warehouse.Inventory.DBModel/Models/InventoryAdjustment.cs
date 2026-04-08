using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Warehouse.Common.Interfaces;

namespace Warehouse.Inventory.DBModel.Models;

/// <summary>
/// Represents an inventory adjustment request with approval workflow.
/// <para>See <see cref="InventoryAdjustmentLine"/>.</para>
/// </summary>
[Table("InventoryAdjustments", Schema = "inventory")]
public sealed class InventoryAdjustment : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the adjustment reason (max 200 characters).
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public required string Reason { get; set; }

    /// <summary>
    /// Gets or sets optional notes (max 2000 characters).
    /// </summary>
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the workflow status (Pending, Approved, Rejected, Applied).
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
    /// Gets or sets the ID of the user who created this adjustment.
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the adjustment was approved.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? ApprovedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who approved this adjustment.
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the adjustment was rejected.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? RejectedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who rejected this adjustment.
    /// </summary>
    public int? RejectedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the rejection reason (max 500 characters).
    /// </summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the adjustment was applied.
    /// </summary>
    [Column(TypeName = "datetime2(7)")]
    public DateTime? AppliedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who applied this adjustment.
    /// </summary>
    public int? AppliedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the optional source stocktake session ID when created from a stocktake.
    /// </summary>
    [ForeignKey(nameof(SourceStocktakeSession))]
    public int? SourceStocktakeSessionId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the source stocktake session.
    /// </summary>
    public StocktakeSession? SourceStocktakeSession { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of adjustment lines.
    /// </summary>
    public ICollection<InventoryAdjustmentLine> Lines { get; set; } = [];
}
