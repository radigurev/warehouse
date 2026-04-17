namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published when an inventory adjustment is applied to stock levels.
/// </summary>
public sealed record InventoryAdjustmentAppliedEvent : ICorrelatedEvent
{
    /// <summary>
    /// Gets the adjustment identifier.
    /// </summary>
    public required int AdjustmentId { get; init; }

    /// <summary>
    /// Gets the user who applied the adjustment.
    /// </summary>
    public required int AppliedByUserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the adjustment was applied.
    /// </summary>
    public required DateTime AppliedAt { get; init; }

    /// <summary>
    /// Gets the correlation ID from the originating HTTP request.
    /// </summary>
    public string? CorrelationId { get; set; }
}
