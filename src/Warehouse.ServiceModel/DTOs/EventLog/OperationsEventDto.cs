namespace Warehouse.ServiceModel.DTOs.EventLog;

/// <summary>
/// Base DTO for operations events across all domains.
/// </summary>
public record OperationsEventDto
{
    /// <summary>
    /// Gets the unique event identifier.
    /// </summary>
    public required long Id { get; init; }

    /// <summary>
    /// Gets the ISA-95 operations domain (Auth, Purchasing, Fulfillment, Inventory, Customers).
    /// </summary>
    public required string Domain { get; init; }

    /// <summary>
    /// Gets the domain-specific event classification.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Gets the entity that was acted upon.
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Gets the primary key of the affected entity in its source domain.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets the user who triggered the event.
    /// </summary>
    public required int UserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred in the source system.
    /// </summary>
    public required DateTime OccurredAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the EventLog service received and persisted the event.
    /// </summary>
    public required DateTime ReceivedAtUtc { get; init; }

    /// <summary>
    /// Gets the correlation ID linking the event to its originating HTTP request.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the JSON payload with event-specific data. Included only in single-event detail responses.
    /// </summary>
    public string? Payload { get; init; }
}
