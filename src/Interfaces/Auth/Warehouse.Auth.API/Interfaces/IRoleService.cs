using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;

namespace Warehouse.Auth.API.Interfaces;

/// <summary>
/// Defines role management operations: CRUD and permission assignment.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Gets a role by ID with its permissions.
    /// </summary>
    Task<Result<RoleDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all roles.
    /// </summary>
    Task<Result<IReadOnlyList<RoleDto>>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new role.
    /// </summary>
    Task<Result<RoleDetailDto>> CreateAsync(CreateRoleRequest request, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    Task<Result<RoleDetailDto>> UpdateAsync(int id, UpdateRoleRequest request, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a role if it is not system-protected and not assigned to users.
    /// </summary>
    Task<Result> DeleteAsync(int id, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the permissions assigned to a role.
    /// </summary>
    Task<Result<IReadOnlyList<PermissionDto>>> GetPermissionsAsync(int roleId, CancellationToken cancellationToken);

    /// <summary>
    /// Assigns permissions to a role.
    /// </summary>
    Task<Result> AssignPermissionsAsync(int roleId, AssignPermissionsRequest request, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a specific permission from a role.
    /// </summary>
    Task<Result> RemovePermissionAsync(int roleId, int permissionId, string? ipAddress, CancellationToken cancellationToken);
}
