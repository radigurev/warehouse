using Microsoft.AspNetCore.Authorization;

namespace Warehouse.Auth.API.Authorization;

/// <summary>
/// Represents an authorization requirement for a specific permission.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the required permission string (e.g., "users:read").
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// Initializes a new instance with the specified permission.
    /// </summary>
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
