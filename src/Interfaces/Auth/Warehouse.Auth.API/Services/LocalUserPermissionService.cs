using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Warehouse.Auth.DBModel;
using Warehouse.Infrastructure.Authorization;

namespace Warehouse.Auth.API.Services;

/// <summary>
/// Resolves user permissions directly from the local Auth database with Redis caching.
/// Used by Auth.API instead of the HTTP-based <see cref="UserPermissionService"/>
/// to avoid self-referential HTTP calls.
/// </summary>
public sealed class LocalUserPermissionService : IUserPermissionService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly AuthDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<LocalUserPermissionService> _logger;

    /// <summary>
    /// Initializes a new instance with database context and cache dependencies.
    /// </summary>
    public LocalUserPermissionService(
        AuthDbContext context,
        IDistributedCache cache,
        ILogger<LocalUserPermissionService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> GetPermissionsAsync(int userId, CancellationToken cancellationToken)
    {
        string cacheKey = UserPermissionService.BuildCacheKey(userId);

        IReadOnlySet<string>? cached = await TryGetFromCacheAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        List<string> permissions = await _context.Users
            .Where(u => u.Id == userId && u.IsActive)
            .SelectMany(u => u.UserRoles)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Resource + ":" + rp.Permission.Action)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        HashSet<string> permissionSet = new(permissions);
        await TrySetCacheAsync(cacheKey, permissionSet, cancellationToken).ConfigureAwait(false);

        return permissionSet;
    }

    private async Task<IReadOnlySet<string>?> TryGetFromCacheAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            byte[]? cached = await _cache.GetAsync(cacheKey, cancellationToken).ConfigureAwait(false);
            if (cached is null)
                return null;

            List<string>? permissions = JsonSerializer.Deserialize<List<string>>(cached);
            return permissions is not null ? new HashSet<string>(permissions) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache read failed for key {CacheKey}", cacheKey);
            return null;
        }
    }

    private async Task TrySetCacheAsync(string cacheKey, HashSet<string> permissions, CancellationToken cancellationToken)
    {
        try
        {
            byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(permissions.ToList());
            DistributedCacheEntryOptions options = new() { AbsoluteExpirationRelativeToNow = CacheDuration };
            await _cache.SetAsync(cacheKey, serialized, options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache write failed for key {CacheKey}", cacheKey);
        }
    }
}
