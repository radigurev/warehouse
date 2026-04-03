namespace Warehouse.ServiceModel.Responses.Auth;

/// <summary>
/// Response payload after creating a user, includes the auto-generated password.
/// </summary>
public sealed record CreateUserResponse
{
    /// <summary>
    /// Gets the created user's ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the created username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets the auto-generated password. Display once to the admin.
    /// </summary>
    /// <remarks>
    /// TODO: Send via email to the user when email service is available.
    /// </remarks>
    public required string GeneratedPassword { get; init; }
}
