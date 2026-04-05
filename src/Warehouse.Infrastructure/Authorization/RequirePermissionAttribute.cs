using Microsoft.AspNetCore.Authorization;

namespace Warehouse.Infrastructure.Authorization;

/// <summary>
/// Applies a permission-based authorization policy to a controller or action.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Gets the required permission string.
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// Initializes a new instance requiring the specified permission (e.g., "customers:read").
    /// </summary>
    public RequirePermissionAttribute(string permission)
        : base(policy: $"Permission:{permission}")
    {
        Permission = permission;
    }
}
