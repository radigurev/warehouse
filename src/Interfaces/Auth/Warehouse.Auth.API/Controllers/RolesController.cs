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
/// Handles role management: CRUD and permission assignment.
/// <para>See <see cref="IRoleService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
public sealed class RolesController : BaseApiController
{
    private readonly IRoleService _roleService;

    /// <summary>
    /// Initializes a new instance with the specified role service.
    /// </summary>
    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>
    /// Gets a paginated list of roles.
    /// </summary>
    [HttpGet]
    [RequirePermission("roles:read")]
    [ProducesResponseType(typeof(PaginatedResponse<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRolesAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationParams.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        PaginationParams pagination = new() { Page = page, PageSize = pageSize };
        Result<PaginatedResponse<RoleDto>> result = await _roleService.GetAllAsync(pagination, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a role by ID with its permissions.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetRoleById")]
    [RequirePermission("roles:read")]
    [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<RoleDetailDto> result = await _roleService.GetByIdAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Creates a new role.
    /// </summary>
    [HttpPost]
    [RequirePermission("roles:write")]
    [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRoleAsync(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        Result<RoleDetailDto> result = await _roleService
            .CreateAsync(request, GetIpAddress(), cancellationToken);

        return ToCreatedResult(result, "GetRoleById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("roles:update")]
    [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRoleAsync(
        int id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        Result<RoleDetailDto> result = await _roleService
            .UpdateAsync(id, request, GetIpAddress(), cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a role if it is not system-protected and not assigned to users.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("roles:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteRoleAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _roleService.DeleteAsync(id, GetIpAddress(), cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Gets the permissions assigned to a role.
    /// </summary>
    [HttpGet("{id:int}/permissions")]
    [RequirePermission("roles:read")]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermissionsAsync(int id, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<PermissionDto>> result = await _roleService.GetPermissionsAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Assigns permissions to a role.
    /// </summary>
    [HttpPost("{id:int}/permissions")]
    [RequirePermission("roles:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignPermissionsAsync(
        int id,
        [FromBody] AssignPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await _roleService
            .AssignPermissionsAsync(id, request, GetIpAddress(), cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Removes a specific permission from a role.
    /// </summary>
    [HttpDelete("{id:int}/permissions/{permissionId:int}")]
    [RequirePermission("roles:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePermissionAsync(
        int id,
        int permissionId,
        CancellationToken cancellationToken)
    {
        Result result = await _roleService.RemovePermissionAsync(id, permissionId, GetIpAddress(), cancellationToken);
        return ToActionResult(result);
    }
}
