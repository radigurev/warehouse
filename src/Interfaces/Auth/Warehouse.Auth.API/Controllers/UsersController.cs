using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses;
using Warehouse.ServiceModel.Responses.Auth;

namespace Warehouse.Auth.API.Controllers;

/// <summary>
/// Handles user management: CRUD, password changes, and role assignment.
/// <para>See <see cref="IUserService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public sealed class UsersController : BaseApiController
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
    /// Gets a paginated list of users. Set includeInactive to true to include deactivated users.
    /// </summary>
    [HttpGet]
    [RequirePermission("users:read")]
    [ProducesResponseType(typeof(PaginatedResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsersAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationParams.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        Result<PaginatedResponse<UserDto>> result = await _userService
            .GetPaginatedAsync(page, pageSize, search, includeInactive, cancellationToken);

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
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUserAsync(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        Result<CreateUserResponse> result = await _userService
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
    /// Resets a user's password to an auto-generated value.
    /// </summary>
    [HttpPost("{id:int}/reset-password")]
    [RequirePermission("users:update")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPasswordAsync(
        int id,
        CancellationToken cancellationToken)
    {
        Result<CreateUserResponse> result = await _userService
            .ResetPasswordAsync(id, GetIpAddress(), cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets the fully resolved permission set for a user (all permissions from all assigned roles).
    /// This is an infrastructure endpoint used by the shared permission validation library.
    /// </summary>
    [HttpGet("{id:int}/permissions")]
    [Authorize]
    [ProducesResponseType(typeof(UserPermissionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserPermissionsAsync(int id, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<string>> result = await _userService.GetResolvedPermissionsAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return ToActionResult(result);

        UserPermissionsResponse response = new()
        {
            UserId = id,
            Permissions = result.Value!
        };

        return Ok(response);
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
