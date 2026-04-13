namespace Warehouse.Infrastructure.Caching;

/// <summary>
/// Provides resilient distributed cache operations for typed list data.
/// <para>Implementations MUST swallow cache failures so that Redis unavailability
/// does not break the calling service's main operation.</para>
/// </summary>
/// <typeparam name="T">The DTO or value type stored in the cache.</typeparam>
public interface ICacheService<T>
{
    /// <summary>
    /// Retrieves a cached list by key, or null on cache miss or failure.
    /// </summary>
    /// <param name="key">The cache key (e.g., <c>auth:permissions:all</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached list, or null if not found or on failure.</returns>
    Task<IReadOnlyList<T>?> GetListAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Serializes and stores a list in the distributed cache with the specified TTL.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="items">The list to cache.</param>
    /// <param name="ttl">Absolute expiration relative to now.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetListAsync(string key, IReadOnlyList<T> items, TimeSpan ttl, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a cached entry by key. Silently succeeds on failure.
    /// </summary>
    /// <param name="key">The cache key to invalidate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateAsync(string key, CancellationToken cancellationToken);
}
