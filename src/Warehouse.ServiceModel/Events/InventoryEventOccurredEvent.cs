namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published by Inventory.API when an inventory operations event occurs.
/// Consumed by EventLog service for centralized operations event logging.
/// </summary>
public sealed record InventoryEventOccurredEvent : ICorrelatedEvent
{
    /// <summary>
    /// Gets the event type (e.g., StockMovementRecorded, AdjustmentApplied, TransferCompleted).
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Gets the entity type (e.g., StockMovement, InventoryAdjustment, WarehouseTransfer).
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Gets the source entity primary key.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets the user who triggered the event.
    /// </summary>
    public required int UserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    public required DateTime OccurredAtUtc { get; init; }

    /// <summary>
    /// Gets the JSON event payload.
    /// </summary>
    public string? Payload { get; init; }

    /// <summary>
    /// Gets the denormalized warehouse name for display.
    /// </summary>
    public string? WarehouseName { get; init; }

    /// <summary>
    /// Gets the denormalized product code and name for display.
    /// </summary>
    public string? ProductInfo { get; init; }

    /// <summary>
    /// Gets the correlation ID from the originating HTTP request.
    /// </summary>
    public string? CorrelationId { get; set; }
}
