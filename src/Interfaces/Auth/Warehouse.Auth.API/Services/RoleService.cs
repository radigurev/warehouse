using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Common.Models;
using Warehouse.Auth.DBModel;
using Warehouse.Auth.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Auth.API.Services;

/// <summary>
/// Implements role management operations.
/// <para>See <see cref="IRoleService"/>.</para>
/// </summary>
public sealed class RoleService : IRoleService
{
    private readonly AuthDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public RoleService(
        AuthDbContext context,
        IAuditService auditService,
        IMapper mapper)
    {
        _context = context;
        _auditService = auditService;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<RoleDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Role? role = await GetRoleWithPermissionsAsync(id, cancellationToken).ConfigureAwait(false);
        if (role is null)
            return Result<RoleDetailDto>.Failure("ROLE_NOT_FOUND", "Role not found.", 404);

        RoleDetailDto dto = _mapper.Map<RoleDetailDto>(role);
        return Result<RoleDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<RoleDto>>> GetAllAsync(
        PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        IQueryable<Role> query = _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<Role> roles = await query
            .Skip(pagination.Skip)
            .Take(pagination.EffectivePageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<RoleDto> dtos = _mapper.Map<IReadOnlyList<RoleDto>>(roles);

        PaginatedResponse<RoleDto> response = new()
        {
            Items = dtos,
            Page = pagination.Page,
            PageSize = pagination.EffectivePageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<RoleDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<RoleDetailDto>> CreateAsync(
        CreateRoleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (await _context.Roles.AnyAsync(r => r.Name == request.Name, cancellationToken).ConfigureAwait(false))
            return Result<RoleDetailDto>.Failure("DUPLICATE_ROLE_NAME", "A role with this name already exists.", 409);

        Role role = new()
        {
            Name = request.Name,
            Description = request.Description
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _auditService.LogAsync(null, "CreateRole", "roles", null, ipAddress, cancellationToken).ConfigureAwait(false);

        Role? loaded = await GetRoleWithPermissionsAsync(role.Id, cancellationToken).ConfigureAwait(false);
        RoleDetailDto dto = _mapper.Map<RoleDetailDto>(loaded!);
        return Result<RoleDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<RoleDetailDto>> UpdateAsync(
        int id,
        UpdateRoleRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        Role? role = await GetRoleWithPermissionsAsync(id, cancellationToken).ConfigureAwait(false);
        if (role is null)
            return Result<RoleDetailDto>.Failure("ROLE_NOT_FOUND", "Role not found.", 404);

        if (role.Name != request.Name &&
            await _context.Roles.AnyAsync(r => r.Name == request.Name, cancellationToken).ConfigureAwait(false))
            return Result<RoleDetailDto>.Failure("DUPLICATE_ROLE_NAME", "A role with this name already exists.", 409);

        role.Name = request.Name;
        role.Description = request.Description;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _auditService.LogAsync(null, "UpdateRole", "roles", null, ipAddress, cancellationToken).ConfigureAwait(false);

        RoleDetailDto dto = _mapper.Map<RoleDetailDto>(role);
        return Result<RoleDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, string? ipAddress, CancellationToken cancellationToken)
    {
        Role? role = await GetRoleWithPermissionsAsync(id, cancellationToken).ConfigureAwait(false);
        if (role is null)
            return Result.Failure("ROLE_NOT_FOUND", "Role not found.", 404);

        if (role.IsSystem)
            return Result.Failure("PROTECTED_ROLE", "The Admin role cannot be deleted.", 400);

        int userCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == id, cancellationToken).ConfigureAwait(false);
        if (userCount > 0)
            return Result.Failure("ROLE_IN_USE", $"Cannot delete role — it is assigned to {userCount} user(s).", 409);

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _auditService.LogAsync(null, "DeleteRole", "roles", null, ipAddress, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PermissionDto>>> GetPermissionsAsync(int roleId, CancellationToken cancellationToken)
    {
        Role? role = await GetRoleWithPermissionsAsync(roleId, cancellationToken).ConfigureAwait(false);
        if (role is null)
            return Result<IReadOnlyList<PermissionDto>>.Failure("ROLE_NOT_FOUND", "Role not found.", 404);

        IReadOnlyList<PermissionDto> permissions = role.RolePermissions
            .Select(rp => _mapper.Map<PermissionDto>(rp.Permission))
            .ToList();

        return Result<IReadOnlyList<PermissionDto>>.Success(permissions);
    }

    /// <inheritdoc />
    public async Task<Result> AssignPermissionsAsync(
        int roleId,
        AssignPermissionsRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        Role? role = await GetRoleWithPermissionsAsync(roleId, cancellationToken).ConfigureAwait(false);
        if (role is null)
            return Result.Failure("ROLE_NOT_FOUND", "Role not found.", 404);

        foreach (int permissionId in request.PermissionIds)
        {
            bool permissionExists = await _context.Permissions.AnyAsync(p => p.Id == permissionId, cancellationToken).ConfigureAwait(false);
            if (!permissionExists)
                return Result.Failure("PERMISSION_NOT_FOUND", "Permission not found.", 404);

            bool alreadyAssigned = role.RolePermissions.Any(rp => rp.PermissionId == permissionId);
            if (!alreadyAssigned)
                _context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _auditService.LogAsync(null, "AssignPermissions", "roles", null, ipAddress, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> RemovePermissionAsync(
        int roleId,
        int permissionId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        Role? role = await GetRoleWithPermissionsAsync(roleId, cancellationToken).ConfigureAwait(false);
        if (role is null)
            return Result.Failure("ROLE_NOT_FOUND", "Role not found.", 404);

        RolePermission? rolePermission = role.RolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId);
        if (rolePermission is null)
            return Result.Failure("PERMISSION_NOT_FOUND", "Permission not found.", 404);

        _context.RolePermissions.Remove(rolePermission);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditService.LogAsync(null, "RemovePermission", "roles", null, ipAddress, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<Role?> GetRoleWithPermissionsAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }
}
