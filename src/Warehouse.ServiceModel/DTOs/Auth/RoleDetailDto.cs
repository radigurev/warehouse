namespace Warehouse.ServiceModel.DTOs.Auth;

/// <summary>
/// Detailed role representation including assigned permissions.
/// </summary>
public sealed record RoleDetailDto
{
    /// <summary>
    /// Gets the role ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the role name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets whether this is a system-protected role.
    /// </summary>
    public required bool IsSystem { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the collection of assigned permissions.
    /// </summary>
    public required IReadOnlyList<PermissionDto> Permissions { get; init; }
}
