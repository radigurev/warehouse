using Warehouse.Common.Models;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses.Auth;

namespace Warehouse.Auth.API.Interfaces;

/// <summary>
/// Defines authentication operations: login, refresh, and logout.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user and returns an access/refresh token pair.
    /// </summary>
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Exchanges a valid refresh token for a new token pair.
    /// </summary>
    Task<Result<RefreshTokenResponse>> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a refresh token on explicit logout.
    /// </summary>
    Task<Result> LogoutAsync(string refreshToken, int userId, string? ipAddress, CancellationToken cancellationToken);
}
