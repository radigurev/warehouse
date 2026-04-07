using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Immutable audit record for fulfillment operations events.
/// <para>Conforms to ISA-95 Operations Event Model.</para>
/// </summary>
public sealed class FulfillmentEvent : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the event type (max 50 characters).
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// Gets or sets the entity type (max 50 characters).
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Gets or sets the user ID (cross-schema ref to auth.Users).
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional JSON payload with before/after state.
    /// </summary>
    public string? Payload { get; set; }
}
