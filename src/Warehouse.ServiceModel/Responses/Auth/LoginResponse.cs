namespace Warehouse.ServiceModel.Responses.Auth;

/// <summary>
/// Response payload containing JWT access and refresh tokens.
/// </summary>
public sealed record LoginResponse
{
    /// <summary>
    /// Gets the JWT access token.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets the opaque refresh token.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Gets the access token expiration timestamp (UTC).
    /// </summary>
    public required DateTime ExpiresAt { get; init; }
}
