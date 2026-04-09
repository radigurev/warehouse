using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Warehouse.Infrastructure.Authorization;

/// <summary>
/// Checks whether the authenticated user has the required permission
/// by resolving the effective permission set via <see cref="IUserPermissionService"/>
/// (Redis cache with Auth.API HTTP fallback).
/// <para>See <see cref="RequirePermissionAttribute"/>, <see cref="PermissionRequirement"/>.</para>
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserPermissionService _permissionService;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    /// <summary>
    /// Initializes a new instance with the user permission service.
    /// </summary>
    public PermissionAuthorizationHandler(
        IUserPermissionService permissionService,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates the permission requirement by resolving the user's permissions at request time.
    /// </summary>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        string? userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
        {
            _logger.LogWarning("Permission check failed — no valid user ID in claims");
            return;
        }

        string[] parts = requirement.Permission.Split(':');
        string resource = parts[0];

        IReadOnlySet<string> permissions = await _permissionService
            .GetPermissionsAsync(userId, CancellationToken.None)
            .ConfigureAwait(false);

        bool hasPermission = permissions.Contains(requirement.Permission)
            || permissions.Contains($"{resource}:all");

        if (hasPermission)
            context.Succeed(requirement);
    }
}
