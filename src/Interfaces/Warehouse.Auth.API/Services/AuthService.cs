using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Warehouse.Auth.API.Configuration;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Common.Models;
using Warehouse.DBModel;
using Warehouse.DBModel.Models.Auth;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses.Auth;

namespace Warehouse.Auth.API.Services;

/// <summary>
/// Implements authentication operations with JWT token management.
/// <para>See <see cref="IAuthService"/>, <see cref="IJwtTokenService"/>, <see cref="IPasswordHasher"/>.</para>
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly WarehouseDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthService> _logger;
    private readonly JwtSettings _jwtSettings;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public AuthService(
        WarehouseDbContext context,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        ILogger<AuthService> logger,
        IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
    }

    /// <inheritdoc />
    public async Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        User? user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for username {Username} from {IpAddress}", request.Username, ipAddress);
            return Result<LoginResponse>.Failure("INVALID_CREDENTIALS", "Invalid username or password.", 401);
        }

        LoginResponse response = await GenerateTokenPairAsync(user, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("User {UserId} logged in from {IpAddress}", user.Id, ipAddress);
        return Result<LoginResponse>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<RefreshTokenResponse>> RefreshAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        RefreshToken? storedToken = await _context.RefreshTokens
            .Include(rt => rt.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken)
            .ConfigureAwait(false);

        if (storedToken is null)
            return Result<RefreshTokenResponse>.Failure("REFRESH_TOKEN_EXPIRED", "Refresh token has expired.", 401);

        if (storedToken.RevokedAt.HasValue)
            return await HandleRevokedTokenAsync(storedToken, ipAddress, cancellationToken).ConfigureAwait(false);

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
            return Result<RefreshTokenResponse>.Failure("REFRESH_TOKEN_EXPIRED", "Refresh token has expired.", 401);

        if (!storedToken.User.IsActive)
            return Result<RefreshTokenResponse>.Failure("INVALID_CREDENTIALS", "Invalid username or password.", 401);

        storedToken.RevokedAt = DateTime.UtcNow;

        LoginResponse tokenPair = await GenerateTokenPairAsync(storedToken.User, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Token refreshed for user {UserId} from {IpAddress}", storedToken.UserId, ipAddress);

        RefreshTokenResponse response = new()
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            ExpiresAt = tokenPair.ExpiresAt
        };

        return Result<RefreshTokenResponse>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result> LogoutAsync(
        string refreshToken,
        int userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        RefreshToken? storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.RevokedAt.HasValue, cancellationToken)
            .ConfigureAwait(false);

        if (storedToken is not null)
            storedToken.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("User {UserId} logged out from {IpAddress}", userId, ipAddress);
        return Result.Success();
    }

    private async Task<LoginResponse> GenerateTokenPairAsync(User user, CancellationToken cancellationToken)
    {
        (string accessToken, DateTime expiresAt) = _jwtTokenService.GenerateAccessToken(user);
        string refreshTokenString = _jwtTokenService.GenerateRefreshToken();

        RefreshToken refreshToken = new()
        {
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = expiresAt
        };
    }

    private async Task<Result<RefreshTokenResponse>> HandleRevokedTokenAsync(
        RefreshToken storedToken,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        List<RefreshToken> activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == storedToken.UserId && !rt.RevokedAt.HasValue)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (RefreshToken token in activeTokens)
            token.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogWarning("Token theft detected for user {UserId} from {IpAddress} — all tokens revoked",
            storedToken.UserId, ipAddress);

        return Result<RefreshTokenResponse>.Failure("REFRESH_TOKEN_REVOKED", "Refresh token has been revoked.", 401);
    }
}
