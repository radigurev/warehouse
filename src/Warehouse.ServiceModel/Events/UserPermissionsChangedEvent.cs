namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published when a user's effective permissions change due to role assignment
/// or role permission modifications. Consuming services SHOULD drop any local
/// in-memory permission state for the affected user.
/// </summary>
public sealed record UserPermissionsChangedEvent : ICorrelatedEvent
{
    /// <summary>
    /// Gets the identifier of the user whose permissions changed.
    /// </summary>
    public required int UserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the change occurred.
    /// </summary>
    public required DateTime OccurredAt { get; init; }

    /// <summary>
    /// Gets the correlation ID from the originating HTTP request.
    /// </summary>
    public string? CorrelationId { get; set; }
}
