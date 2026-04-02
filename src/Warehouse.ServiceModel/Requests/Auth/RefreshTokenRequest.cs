namespace Warehouse.ServiceModel.Requests.Auth;

/// <summary>
/// Request payload for refreshing an access token.
/// </summary>
public sealed record RefreshTokenRequest
{
    /// <summary>
    /// Gets the refresh token string.
    /// </summary>
    public required string RefreshToken { get; init; }
}
