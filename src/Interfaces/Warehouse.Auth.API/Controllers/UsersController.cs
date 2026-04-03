using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Auth.API.Authorization;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Auth.API.Controllers;

/// <summary>
/// Handles user management: CRUD, password changes, and role assignment.
/// <para>See <see cref="IUserService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public sealed class UsersController : BaseAuthController
{
    private readonly IUserService _userService;

    /// <summary>
    /// Initializes a new instance with the specified user service.
    /// </summary>
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Gets a paginated list of active users.
    /// </summary>
    [HttpGet]
    [RequirePermission("users:read")]
    [ProducesResponseType(typeof(PaginatedResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsersAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        Result<PaginatedResponse<UserDto>> result = await _userService
            .GetPaginatedAsync(page, pageSize, search, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetUserById")]
    [RequirePermission("users:read")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<UserDetailDto> result = await _userService.GetByIdAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    [HttpPost]
    [RequirePermission("users:write")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUserAsync(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        Result<UserDetailDto> result = await _userService
            .CreateAsync(request, GetIpAddress(), cancellationToken);

        return ToCreatedResult(result, "GetUserById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Updates a user's profile fields.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("users:update")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUserAsync(
        int id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        Result<UserDetailDto> result = await _userService
            .UpdateAsync(id, request, GetIpAddress(), cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deactivates (soft-deletes) a user.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("users:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUserAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _userService.DeactivateAsync(id, GetIpAddress(), cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Changes a user's password. Allowed for own user or users with users:update permission.
    /// </summary>
    [HttpPut("{id:int}/password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePasswordAsync(
        int id,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        int currentUserId = GetCurrentUserId();
        if (currentUserId != id)
        {
            bool hasPermission = await HasPermissionAsync("users:update", cancellationToken);
            if (!hasPermission)
                return Forbid();
        }

        Result result = await _userService
            .ChangePasswordAsync(id, request, GetIpAddress(), cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets the roles assigned to a user.
    /// </summary>
    [HttpGet("{id:int}/roles")]
    [RequirePermission("users:read")]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserRolesAsync(int id, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<RoleDto>> result = await _userService.GetRolesAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Assigns roles to a user.
    /// </summary>
    [HttpPost("{id:int}/roles")]
    [RequirePermission("users:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRolesAsync(
        int id,
        [FromBody] AssignRolesRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await _userService
            .AssignRolesAsync(id, request, GetIpAddress(), cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Removes a specific role from a user.
    /// </summary>
    [HttpDelete("{id:int}/roles/{roleId:int}")]
    [RequirePermission("users:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRoleAsync(int id, int roleId, CancellationToken cancellationToken)
    {
        Result result = await _userService.RemoveRoleAsync(id, roleId, GetIpAddress(), cancellationToken);
        return ToActionResult(result);
    }
}
