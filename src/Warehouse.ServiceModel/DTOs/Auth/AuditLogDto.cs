namespace Warehouse.ServiceModel.DTOs.Auth;

/// <summary>
/// Represents a single audit log entry.
/// </summary>
public sealed record AuditLogDto
{
    /// <summary>
    /// Gets the log entry ID.
    /// </summary>
    public required long Id { get; init; }

    /// <summary>
    /// Gets the user ID that performed the action, or null.
    /// </summary>
    public int? UserId { get; init; }

    /// <summary>
    /// Gets the action performed.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the resource affected.
    /// </summary>
    public required string Resource { get; init; }

    /// <summary>
    /// Gets the optional JSON details.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Gets the IP address of the originator.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAt { get; init; }
}
