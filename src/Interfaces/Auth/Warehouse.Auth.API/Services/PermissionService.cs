using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Warehouse.Auth.API.Interfaces;
using Warehouse.Common.Models;
using Warehouse.Auth.DBModel;
using Warehouse.Auth.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Auth.API.Services;

/// <summary>
/// Implements permission management operations with Redis caching.
/// <para>See <see cref="IPermissionService"/>.</para>
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private const string CacheKey = "auth:permissions:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly AuthDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public PermissionService(
        AuthDbContext context,
        IAuditService auditService,
        IMapper mapper,
        IDistributedCache cache)
    {
        _context = context;
        _auditService = auditService;
        _mapper = mapper;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<PermissionDto>>> GetAllAsync(
        PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PermissionDto>? cached = await GetCachedListAsync(cancellationToken).ConfigureAwait(false);

        if (cached is not null)
            return PaginateFromList(cached, pagination);

        List<Permission> permissions = await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Resource).ThenBy(p => p.Action)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<PermissionDto> allDtos = _mapper.Map<IReadOnlyList<PermissionDto>>(permissions);
        await SetCacheAsync(allDtos, cancellationToken).ConfigureAwait(false);

        return PaginateFromList(allDtos, pagination);
    }

    /// <inheritdoc />
    public async Task<Result<PermissionDto>> CreateAsync(
        CreatePermissionRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        bool exists = await _context.Permissions
            .AnyAsync(p => p.Resource == request.Resource && p.Action == request.Action, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
            return Result<PermissionDto>.Failure("DUPLICATE_PERMISSION", "A permission with this resource and action already exists.", 409);

        Permission permission = new()
        {
            Resource = request.Resource,
            Action = request.Action,
            Description = request.Description
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);
        await _auditService.LogAsync(null, "CreatePermission", "permissions", null, ipAddress, cancellationToken).ConfigureAwait(false);

        PermissionDto dto = _mapper.Map<PermissionDto>(permission);
        return Result<PermissionDto>.Success(dto);
    }

    /// <summary>
    /// Paginates an in-memory list of DTOs.
    /// </summary>
    private static Result<PaginatedResponse<PermissionDto>> PaginateFromList(
        IReadOnlyList<PermissionDto> items,
        PaginationParams pagination)
    {
        PaginatedResponse<PermissionDto> response = new()
        {
            Items = items.Skip(pagination.Skip).Take(pagination.EffectivePageSize).ToList(),
            Page = pagination.Page,
            PageSize = pagination.EffectivePageSize,
            TotalCount = items.Count
        };

        return Result<PaginatedResponse<PermissionDto>>.Success(response);
    }

    /// <summary>
    /// Attempts to read the full permissions list from cache.
    /// </summary>
    private async Task<IReadOnlyList<PermissionDto>?> GetCachedListAsync(CancellationToken cancellationToken)
    {
        byte[]? cached = await _cache.GetAsync(CacheKey, cancellationToken).ConfigureAwait(false);
        return cached is null ? null : JsonSerializer.Deserialize<List<PermissionDto>>(cached);
    }

    /// <summary>
    /// Stores the full permissions list in cache.
    /// </summary>
    private async Task SetCacheAsync(IReadOnlyList<PermissionDto> items, CancellationToken cancellationToken)
    {
        byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(items);
        DistributedCacheEntryOptions options = new() { AbsoluteExpirationRelativeToNow = CacheDuration };
        await _cache.SetAsync(CacheKey, serialized, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the permissions list from cache.
    /// </summary>
    private async Task InvalidateCacheAsync(CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(CacheKey, cancellationToken).ConfigureAwait(false);
    }
}
