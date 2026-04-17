namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published by Auth.API when an audit entry is recorded.
/// Consumed by EventLog service for centralized operations event logging.
/// </summary>
public sealed record AuthAuditLoggedEvent : ICorrelatedEvent
{
    /// <summary>
    /// Gets the user who performed the action (nullable for system actions).
    /// </summary>
    public required int? UserId { get; init; }

    /// <summary>
    /// Gets the auth action performed (e.g., CreateUser, Login, AssignRole).
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the auth resource acted upon (e.g., users, roles, permissions).
    /// </summary>
    public required string Resource { get; init; }

    /// <summary>
    /// Gets the JSON details of the action.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Gets the client IP address that initiated the action.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Gets the denormalized username for display without cross-service lookup.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the action occurred.
    /// </summary>
    public required DateTime OccurredAtUtc { get; init; }

    /// <summary>
    /// Gets the correlation ID from the originating HTTP request.
    /// </summary>
    public string? CorrelationId { get; set; }
}
