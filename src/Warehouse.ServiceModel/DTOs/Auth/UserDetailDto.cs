namespace Warehouse.ServiceModel.DTOs.Auth;

/// <summary>
/// Detailed user representation including assigned roles.
/// </summary>
public sealed record UserDetailDto
{
    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets the email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets the first name.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// Gets the last name.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// Gets whether the user is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the UTC last-update timestamp.
    /// </summary>
    public required DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Gets the collection of assigned roles.
    /// </summary>
    public required IReadOnlyList<RoleDto> Roles { get; init; }
}
