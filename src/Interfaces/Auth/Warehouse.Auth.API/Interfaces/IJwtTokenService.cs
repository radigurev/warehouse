using Warehouse.Auth.DBModel.Models;

namespace Warehouse.Auth.API.Interfaces;

/// <summary>
/// Defines operations for generating and managing JWT tokens.
/// <para>See <see cref="User"/>.</para>
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token for the specified user.
    /// </summary>
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);

    /// <summary>
    /// Generates a cryptographically random refresh token string.
    /// </summary>
    string GenerateRefreshToken();
}
