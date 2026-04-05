namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Inventory adjustment header representation for list views.
/// </summary>
public sealed record InventoryAdjustmentDto
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
}
