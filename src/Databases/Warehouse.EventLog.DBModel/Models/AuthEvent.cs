namespace Warehouse.EventLog.DBModel.Models;

/// <summary>
/// Operations event for the Auth/Personnel domain.
/// Extends the base with auth-specific fields.
/// </summary>
public class AuthEvent : OperationsEvent
{
    /// <summary>
    /// Gets or sets the auth action performed (e.g., CreateUser, AssignRole, Login).
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Gets or sets the auth resource acted upon (e.g., users, roles, permissions).
    /// </summary>
    public required string Resource { get; set; }

    /// <summary>
    /// Gets or sets the client IP address that initiated the action.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the denormalized username for display without cross-service lookup.
    /// </summary>
    public string? Username { get; set; }
}
