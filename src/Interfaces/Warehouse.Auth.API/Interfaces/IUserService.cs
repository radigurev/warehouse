using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Auth.API.Interfaces;

/// <summary>
/// Defines user management operations: CRUD, password changes, and role assignment.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    Task<Result<UserDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a paginated list of active users.
    /// </summary>
    Task<Result<PaginatedResponse<UserDto>>> GetPaginatedAsync(int page, int pageSize, string? searchTerm, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new user.
    /// </summary>
    Task<Result<UserDetailDto>> CreateAsync(CreateUserRequest request, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing user's profile fields.
    /// </summary>
    Task<Result<UserDetailDto>> UpdateAsync(int id, UpdateUserRequest request, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes (deactivates) a user.
    /// </summary>
    Task<Result> DeactivateAsync(int id, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Changes a user's password after verifying the current one.
    /// </summary>
    Task<Result> ChangePasswordAsync(int id, ChangePasswordRequest request, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the roles assigned to a user.
    /// </summary>
    Task<Result<IReadOnlyList<RoleDto>>> GetRolesAsync(int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Assigns roles to a user.
    /// </summary>
    Task<Result> AssignRolesAsync(int userId, AssignRolesRequest request, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a specific role from a user.
    /// </summary>
    Task<Result> RemoveRoleAsync(int userId, int roleId, string? ipAddress, CancellationToken cancellationToken);
}
