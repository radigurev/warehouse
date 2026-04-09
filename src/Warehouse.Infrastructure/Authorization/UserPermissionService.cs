using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Warehouse.ServiceModel.Responses.Auth;

namespace Warehouse.Infrastructure.Authorization;

/// <summary>
/// Resolves user permissions via Redis cache with HTTP fallback to Auth.API.
/// <para>See <see cref="IUserPermissionService"/>.</para>
/// </summary>
public sealed class UserPermissionService : IUserPermissionService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IDistributedCache _cache;
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserPermissionService> _logger;

    /// <summary>
    /// Initializes a new instance with cache, HTTP client, and logger dependencies.
    /// </summary>
    public UserPermissionService(
        IDistributedCache cache,
        HttpClient httpClient,
        ILogger<UserPermissionService> logger)
    {
        _cache = cache;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> GetPermissionsAsync(int userId, CancellationToken cancellationToken)
    {
        string cacheKey = BuildCacheKey(userId);

        IReadOnlySet<string>? cached = await TryGetFromCacheAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        IReadOnlySet<string>? fetched = await TryFetchFromAuthApiAsync(userId, cancellationToken).ConfigureAwait(false);
        if (fetched is not null)
        {
            await TrySetCacheAsync(cacheKey, fetched, cancellationToken).ConfigureAwait(false);
            return fetched;
        }

        _logger.LogWarning("Permission resolution failed for user {UserId} — both Redis and Auth.API unavailable, denying access", userId);
        return new HashSet<string>();
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

    private async Task<IReadOnlySet<string>?> TryFetchFromAuthApiAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            UserPermissionsResponse? response = await _httpClient
                .GetFromJsonAsync<UserPermissionsResponse>($"api/v1/users/{userId}/permissions", cancellationToken)
                .ConfigureAwait(false);

            if (response?.Permissions is null)
                return null;

            return new HashSet<string>(response.Permissions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auth.API permission fetch failed for user {UserId}", userId);
            return null;
        }
    }

    private async Task TrySetCacheAsync(string cacheKey, IReadOnlySet<string> permissions, CancellationToken cancellationToken)
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

    /// <summary>
    /// Builds the Redis cache key for a user's permissions.
    /// </summary>
    public static string BuildCacheKey(int userId)
    {
        return $"auth:user:{userId}:permissions";
    }
}
