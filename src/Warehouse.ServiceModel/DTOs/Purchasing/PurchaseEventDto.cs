namespace Warehouse.ServiceModel.DTOs.Purchasing;

/// <summary>
/// Represents an immutable procurement operations event record.
/// </summary>
public sealed record PurchaseEventDto
{
    /// <summary>
    /// Gets the event ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the event type.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Gets the entity type.
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public required int UserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    public required DateTime OccurredAtUtc { get; init; }

    /// <summary>
    /// Gets the optional JSON payload.
    /// </summary>
    public string? Payload { get; init; }
}
