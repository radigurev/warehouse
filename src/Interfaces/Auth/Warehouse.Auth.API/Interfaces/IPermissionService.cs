using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Auth.API.Interfaces;

/// <summary>
/// Defines permission management operations.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Gets a paginated list of permissions.
    /// </summary>
    Task<Result<PaginatedResponse<PermissionDto>>> GetAllAsync(PaginationParams pagination, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new permission.
    /// </summary>
    Task<Result<PermissionDto>> CreateAsync(CreatePermissionRequest request, string? ipAddress, CancellationToken cancellationToken);
}
