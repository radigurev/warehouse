using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Common.Models;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses.Auth;

namespace Warehouse.Auth.API.Controllers;

/// <summary>
/// Handles authentication operations: login, token refresh, and logout.
/// <para>See <see cref="IAuthService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : BaseAuthController
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance with the specified auth service.
    /// </summary>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates a user and returns JWT access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        Result<LoginResponse> result = await _authService
            .LoginAsync(request, GetIpAddress(), cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new access/refresh token pair.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        Result<RefreshTokenResponse> result = await _authService
            .RefreshAsync(request, GetIpAddress(), cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Revokes the specified refresh token on explicit logout.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();
        Result result = await _authService
            .LogoutAsync(request.RefreshToken, userId, GetIpAddress(), cancellationToken);

        return ToActionResult(result);
    }
}
