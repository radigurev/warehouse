namespace Warehouse.ServiceModel.Requests.Auth;

/// <summary>
/// Request payload for creating a new user.
/// </summary>
public sealed record CreateUserRequest
{
    /// <summary>
    /// Gets the desired username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets the email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets the password.
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// Gets the first name.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// Gets the last name.
    /// </summary>
    public required string LastName { get; init; }
}
