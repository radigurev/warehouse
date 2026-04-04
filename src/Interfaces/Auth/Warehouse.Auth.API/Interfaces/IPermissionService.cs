using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;

namespace Warehouse.Auth.API.Interfaces;

/// <summary>
/// Defines permission management operations.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Gets all permissions.
    /// </summary>
    Task<Result<IReadOnlyList<PermissionDto>>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new permission.
    /// </summary>
    Task<Result<PermissionDto>> CreateAsync(CreatePermissionRequest request, string? ipAddress, CancellationToken cancellationToken);
}
