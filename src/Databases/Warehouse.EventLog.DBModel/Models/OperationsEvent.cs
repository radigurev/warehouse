namespace Warehouse.EventLog.DBModel.Models;

/// <summary>
/// Base entity for all operations events across ISA-95 domains.
/// Uses TPT inheritance with domain-specific derived types.
/// <para>Conforms to ISA-95 Part 2 — Operations Event Model.</para>
/// </summary>
public class OperationsEvent
{
    /// <summary>
    /// Gets or sets the unique event identifier (BIGINT IDENTITY).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the ISA-95 operations domain discriminator.
    /// </summary>
    public required string Domain { get; set; }

    /// <summary>
    /// Gets or sets the domain-specific event classification.
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// Gets or sets the entity that was acted upon.
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// Gets or sets the primary key of the affected entity in its source domain.
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Gets or sets the user who triggered the event (cross-schema reference to auth.Users).
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the event occurred in the source system.
    /// </summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the EventLog service received and persisted the event.
    /// </summary>
    public DateTime ReceivedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the JSON representation of event-specific data.
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// Gets or sets the infrastructure correlation ID linking the event to its originating HTTP request.
    /// </summary>
    public string? CorrelationId { get; set; }
}
