namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published by Purchasing.API when a procurement operations event is recorded.
/// Consumed by EventLog service for centralized operations event logging.
/// </summary>
public sealed record PurchaseEventOccurredEvent
{
    /// <summary>
    /// Gets the event type (e.g., PurchaseOrderCreated, GoodsReceiptCompleted).
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Gets the entity type (e.g., PurchaseOrder, GoodsReceipt).
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
    /// Gets the denormalized supplier name for display.
    /// </summary>
    public string? SupplierName { get; init; }

    /// <summary>
    /// Gets the document reference (PO number, GR number, SR number).
    /// </summary>
    public string? DocumentNumber { get; init; }

    /// <summary>
    /// Gets the correlation ID from the originating HTTP request.
    /// </summary>
    public string? CorrelationId { get; init; }
}
