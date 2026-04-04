using Microsoft.AspNetCore.Authorization;

namespace Warehouse.Customers.API.Authorization;

/// <summary>
/// Marks an endpoint as requiring a specific permission (e.g., "customers:read").
/// <para>See <see cref="PermissionAuthorizationHandler"/>.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Gets the required permission string.
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// Initializes a new instance requiring the specified permission.
    /// </summary>
    public RequirePermissionAttribute(string permission)
        : base(policy: $"Permission:{permission}")
    {
        Permission = permission;
    }
}
