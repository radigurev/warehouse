using System.Text.Json;
using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Auth.DBModel;
using Warehouse.Auth.DBModel.Models;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Auth.API.Services;

/// <summary>
/// Implements role management operations.
/// <para>See <see cref="IRoleService"/>.</para>
/// </summary>
public sealed class RoleService : IRoleService
{
    private const string CacheKey = "auth:roles:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly AuthDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<RoleService> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public RoleService(
        AuthDbContext context,
        IAuditService auditService,
        IMapper mapper,
        IDistributedCache cache,
        IPublishEndpoint publishEndpoint,
        ILogger<RoleService> logger)
    {
        _context = context;
        _auditService = auditService;
        _mapper = mapper;
        _cache = cache;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
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
        IReadOnlyList<RoleDto>? cached = await GetCachedListAsync(cancellationToken).ConfigureAwait(false);

        if (cached is not null)
            return PaginateFromList(cached, pagination);

        List<Role> roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<RoleDto> allDtos = _mapper.Map<IReadOnlyList<RoleDto>>(roles);
        await SetCacheAsync(allDtos, cancellationToken).ConfigureAwait(false);

        return PaginateFromList(allDtos, pagination);
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
        await InvalidateRolesCacheAsync(cancellationToken).ConfigureAwait(false);

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
        await InvalidateRolesCacheAsync(cancellationToken).ConfigureAwait(false);

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
        await InvalidateRolesCacheAsync(cancellationToken).ConfigureAwait(false);
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
        await InvalidateRoleUsersCacheAsync(roleId, cancellationToken).ConfigureAwait(false);
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
        await InvalidateRoleUsersCacheAsync(roleId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// Paginates an in-memory list of DTOs.
    /// </summary>
    private static Result<PaginatedResponse<RoleDto>> PaginateFromList(
        IReadOnlyList<RoleDto> items,
        PaginationParams pagination)
    {
        PaginatedResponse<RoleDto> response = new()
        {
            Items = items.Skip(pagination.Skip).Take(pagination.EffectivePageSize).ToList(),
            Page = pagination.Page,
            PageSize = pagination.EffectivePageSize,
            TotalCount = items.Count
        };

        return Result<PaginatedResponse<RoleDto>>.Success(response);
    }

    /// <summary>
    /// Attempts to read the full roles list from cache.
    /// </summary>
    private async Task<IReadOnlyList<RoleDto>?> GetCachedListAsync(CancellationToken cancellationToken)
    {
        byte[]? cached = await _cache.GetAsync(CacheKey, cancellationToken).ConfigureAwait(false);
        return cached is null ? null : JsonSerializer.Deserialize<List<RoleDto>>(cached);
    }

    /// <summary>
    /// Stores the full roles list in cache.
    /// </summary>
    private async Task SetCacheAsync(IReadOnlyList<RoleDto> items, CancellationToken cancellationToken)
    {
        byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(items);
        DistributedCacheEntryOptions options = new() { AbsoluteExpirationRelativeToNow = CacheDuration };
        await _cache.SetAsync(CacheKey, serialized, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the roles list from cache.
    /// </summary>
    private async Task InvalidateRolesCacheAsync(CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(CacheKey, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Role?> GetRoleWithPermissionsAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Invalidates Redis permission caches for all users assigned to the specified role
    /// and publishes a change event for each affected user.
    /// </summary>
    private async Task InvalidateRoleUsersCacheAsync(int roleId, CancellationToken cancellationToken)
    {
        List<int> affectedUserIds = await _context.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (int userId in affectedUserIds)
        {
            string cacheKey = UserPermissionService.BuildCacheKey(userId);
            await _cache.RemoveAsync(cacheKey, cancellationToken).ConfigureAwait(false);

            try
            {
                await _publishEndpoint.Publish(new UserPermissionsChangedEvent
                {
                    UserId = userId,
                    OccurredAt = DateTime.UtcNow
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish UserPermissionsChangedEvent for user {UserId}", userId);
            }
        }
    }
}
