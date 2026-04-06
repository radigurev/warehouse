using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Auth.API.Controllers;

/// <summary>
/// Handles permission CRUD operations.
/// <para>See <see cref="IPermissionService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/permissions")]
[RequirePermission("roles:read")]
public sealed class PermissionsController : BaseApiController
{
    private readonly IPermissionService _permissionService;

    /// <summary>
    /// Initializes a new instance with the specified permission service.
    /// </summary>
    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>
    /// Gets a paginated list of permissions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPermissionsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationParams.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        PaginationParams pagination = new() { Page = page, PageSize = pageSize };
        Result<PaginatedResponse<PermissionDto>> result = await _permissionService.GetAllAsync(pagination, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Creates a new permission.
    /// </summary>
    [HttpPost]
    [RequirePermission("roles:write")]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePermissionAsync(
        [FromBody] CreatePermissionRequest request,
        CancellationToken cancellationToken)
    {
        Result<PermissionDto> result = await _permissionService
            .CreateAsync(request, GetIpAddress(), cancellationToken);

        if (result.IsSuccess)
            return StatusCode(StatusCodes.Status201Created, result.Value);

        return ToActionResult(result);
    }
}
