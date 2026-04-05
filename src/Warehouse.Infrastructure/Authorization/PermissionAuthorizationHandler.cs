using Microsoft.AspNetCore.Authorization;

namespace Warehouse.Infrastructure.Authorization;

/// <summary>
/// Checks whether the authenticated user has the required permission
/// by reading permission claims embedded in the JWT token.
/// <para>See <see cref="RequirePermissionAttribute"/>, <see cref="PermissionRequirement"/>.</para>
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <summary>
    /// Evaluates the permission requirement against the JWT permission claims.
    /// </summary>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        string[] parts = requirement.Permission.Split(':');
        string resource = parts[0];

        IEnumerable<string> permissions = context.User
            .FindAll("permission")
            .Select(c => c.Value);

        bool hasPermission = permissions.Any(p =>
            p == requirement.Permission ||
            p == $"{resource}:all");

        if (hasPermission)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
