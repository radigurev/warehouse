namespace Warehouse.ServiceModel.DTOs.Auth;

/// <summary>
/// Lightweight role representation for list views.
/// </summary>
public sealed record RoleDto
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
}
