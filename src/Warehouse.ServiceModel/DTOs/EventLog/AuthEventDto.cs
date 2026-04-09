namespace Warehouse.ServiceModel.DTOs.EventLog;

/// <summary>
/// DTO for Auth domain operations events with personnel-specific fields.
/// </summary>
public sealed record AuthEventDto : OperationsEventDto
{
    /// <summary>
    /// Gets the auth action performed (e.g., CreateUser, AssignRole, Login).
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the auth resource acted upon (e.g., users, roles, permissions).
    /// </summary>
    public required string Resource { get; init; }

    /// <summary>
    /// Gets the client IP address that initiated the action.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Gets the denormalized username for display.
    /// </summary>
    public string? Username { get; init; }
}
