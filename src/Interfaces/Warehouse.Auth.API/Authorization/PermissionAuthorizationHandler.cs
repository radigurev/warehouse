using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Warehouse.DBModel;

namespace Warehouse.Auth.API.Authorization;

/// <summary>
/// Checks whether the authenticated user has the required permission
/// by resolving user roles and their assigned permissions from the database.
/// <para>See <see cref="RequirePermissionAttribute"/>, <see cref="PermissionRequirement"/>.</para>
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance with the specified scope factory.
    /// </summary>
    public PermissionAuthorizationHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Evaluates the permission requirement against the current user.
    /// </summary>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        string? userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return;

        using IServiceScope scope = _scopeFactory.CreateScope();
        WarehouseDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        bool hasPermission = await CheckPermissionAsync(dbContext, userId, requirement.Permission)
            .ConfigureAwait(false);

        if (hasPermission)
            context.Succeed(requirement);
    }

    private static async Task<bool> CheckPermissionAsync(
        WarehouseDbContext dbContext,
        int userId,
        string permission)
    {
        string[] parts = permission.Split(':');
        string resource = parts[0];
        string action = parts.Length > 1 ? parts[1] : "all";

        bool hasPermission = await dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission)
            .AnyAsync(p =>
                (p.Resource == resource && (p.Action == action || p.Action == "all")) ||
                (p.Resource == resource && p.Action == "all"))
            .ConfigureAwait(false);

        return hasPermission;
    }
}
