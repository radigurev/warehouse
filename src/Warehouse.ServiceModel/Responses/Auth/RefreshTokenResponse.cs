namespace Warehouse.ServiceModel.Responses.Auth;

/// <summary>
/// Response payload for a token refresh operation.
/// </summary>
public sealed record RefreshTokenResponse
{
    /// <summary>
    /// Gets the new JWT access token.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets the new opaque refresh token.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Gets the new access token expiration timestamp (UTC).
    /// </summary>
    public required DateTime ExpiresAt { get; init; }
}
