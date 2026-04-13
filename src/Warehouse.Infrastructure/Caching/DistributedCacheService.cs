using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Warehouse.Infrastructure.Caching;

/// <summary>
/// Wraps <see cref="IDistributedCache"/> with JSON serialization and resilient
/// try-catch handling. Redis unavailability is logged as a warning and never
/// propagated to the caller.
/// <para>See <see cref="ICacheService{T}"/>.</para>
/// </summary>
/// <typeparam name="T">The DTO or value type stored in the cache.</typeparam>
public sealed class DistributedCacheService<T> : ICacheService<T>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService<T>> _logger;

    /// <summary>
    /// Initializes a new instance with the distributed cache and logger.
    /// </summary>
    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService<T>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>?> GetListAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            byte[]? cached = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);

            if (cached is null)
                return null;

            List<T>? deserialized = JsonSerializer.Deserialize<List<T>>(cached);
            return deserialized;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache read failed for key {CacheKey}", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetListAsync(
        string key,
        IReadOnlyList<T> items,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        try
        {
            byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(items);

            DistributedCacheEntryOptions options = new()
            {
                AbsoluteExpirationRelativeToNow = ttl
            };

            await _cache.SetAsync(key, serialized, options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache write failed for key {CacheKey}", key);
        }
    }

    /// <inheritdoc />
    public async Task InvalidateAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache invalidation failed for key {CacheKey}", key);
        }
    }
}
