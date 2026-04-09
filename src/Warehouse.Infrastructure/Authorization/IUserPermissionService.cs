namespace Warehouse.Infrastructure.Authorization;

/// <summary>
/// Resolves the effective permission set for a user at request time.
/// Checks Redis cache first, falls back to Auth.API HTTP call on cache miss.
/// Fails closed (denies access) when both Redis and Auth.API are unavailable.
/// </summary>
public interface IUserPermissionService
{
    /// <summary>
    /// Gets the resolved permission strings for the specified user.
    /// Returns an empty set and logs a warning when both cache and Auth.API are unreachable (fail-closed).
    /// </summary>
    Task<IReadOnlySet<string>> GetPermissionsAsync(int userId, CancellationToken cancellationToken);
}
