namespace Warehouse.ServiceModel.Requests.Auth;

/// <summary>
/// Request payload for user authentication.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>
    /// Gets the username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets the password.
    /// </summary>
    public required string Password { get; init; }
}
