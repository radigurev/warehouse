namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Full inventory adjustment representation including lines and approval information.
/// </summary>
public sealed record InventoryAdjustmentDetailDto
{
    /// <summary>
    /// Gets the adjustment ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the workflow status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the adjustment reason.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the optional notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the ID of the user who created this adjustment.
    /// </summary>
    public required int CreatedByUserId { get; init; }

    /// <summary>
    /// Gets the optional UTC approval timestamp.
    /// </summary>
    public DateTime? ApprovedAtUtc { get; init; }

    /// <summary>
    /// Gets the optional ID of the approving user.
    /// </summary>
    public int? ApprovedByUserId { get; init; }

    /// <summary>
    /// Gets the optional UTC rejection timestamp.
    /// </summary>
    public DateTime? RejectedAtUtc { get; init; }

    /// <summary>
    /// Gets the optional ID of the rejecting user.
    /// </summary>
    public int? RejectedByUserId { get; init; }

    /// <summary>
    /// Gets the optional rejection reason.
    /// </summary>
    public string? RejectionReason { get; init; }

    /// <summary>
    /// Gets the optional UTC applied timestamp.
    /// </summary>
    public DateTime? AppliedAtUtc { get; init; }

    /// <summary>
    /// Gets the optional ID of the user who applied.
    /// </summary>
    public int? AppliedByUserId { get; init; }

    /// <summary>
    /// Gets the optional source stocktake session ID when created from a stocktake.
    /// </summary>
    public int? SourceStocktakeSessionId { get; init; }

    /// <summary>
    /// Gets the collection of adjustment lines.
    /// </summary>
    public required IReadOnlyList<InventoryAdjustmentLineDto> Lines { get; init; }
}
