# CHG-REFAC-007 — Decorator Pattern for Cache, Events & Resilience

> Status: Draft
> Last updated: 2026-04-09
> Owner: TBD
> Priority: P2

## 1. Context & Scope

**Why this change is needed:**
Three cross-cutting concerns are implemented inline across multiple services: (1) Cache management — 4+ services (`RoleService`, `PermissionService`, `CustomerCategoryService`, `LocalUserPermissionService`) duplicate identical `GetCachedListAsync`/`SetCacheAsync` methods with JSON serialization and try-catch-log wrapping. (2) Event publishing — 7+ services wrap `IPublishEndpoint.Publish(...)` in identical try-catch blocks with logging. (3) HTTP resilience — Polly retry/circuit-breaker options are configured inline in two `ServiceCollectionExtensions` methods with slightly different values. The Decorator pattern wraps these concerns once, so every service benefits without knowing about the cross-cutting logic.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [ ] Frontend changes
- [ ] Configuration changes

## 2. Behavior (RFC 2119)

### Cache Decorator

- The system MUST provide an `ICacheService<T>` interface with methods: `Task<IReadOnlyList<T>?> GetListAsync(string key, CancellationToken ct)` and `Task SetListAsync(string key, IReadOnlyList<T> items, TimeSpan ttl, CancellationToken ct)`.
- The implementation MUST handle JSON serialization/deserialization internally.
- The implementation MUST wrap all `IDistributedCache` calls in try-catch, logging warnings on failure and returning `null` (cache miss) — Redis unavailability MUST NOT break the main operation.
- The system MUST provide a `CachedListService<T>` decorator that wraps any list-fetching operation with cache-aside logic: check cache → if miss, fetch from source → cache result.
- Services MUST inject `ICacheService<T>` instead of `IDistributedCache` directly.
- The cache key convention (`{service}:{entity}:all`) and TTL configuration MUST be maintained.
- Adding a new cacheable entity MUST require only: injecting `ICacheService<TDto>` and calling `GetListAsync`/`SetListAsync`.

### Resilient Event Publisher

- The system MUST provide an `IResilientPublisher` interface wrapping `IPublishEndpoint` with method: `Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct) where TEvent : class`.
- The implementation MUST wrap the publish call in try-catch with structured warning logging.
- The implementation SHOULD support a configurable retry policy (default: no retry, fire-and-forget).
- Services MUST inject `IResilientPublisher` instead of `IPublishEndpoint` directly.
- The implementation MUST log: event type name, entity ID (if extractable), and the exception message.
- Adding new event publishing MUST require only: calling `_publisher.PublishAsync(event, ct)` — no try-catch in calling code.

### Resilience Profile Registry

- The system MUST define named resilience profiles as constants: `"Standard"`, `"Permissive"`, `"Strict"`.
- Each profile MUST specify: retry attempts, retry delay, circuit breaker throughput, break duration, attempt timeout, total timeout.
- `AddWarehouseHttpClient<T, TImpl>()` MUST accept an optional profile name parameter (default: `"Standard"`).
- `AddWarehousePermissionValidation()` MUST use the `"Permissive"` profile (fewer retries, shorter timeouts).
- Adding a new resilience profile MUST require only: defining the values in the registry — no changes to `ServiceCollectionExtensions`.

## 3. Validation Rules

No new validation rules — this is a cross-cutting infrastructure refactor.

## 4. Error Rules

No new error rules — decorators preserve existing error behavior.

## 5. Versioning Notes

**API version impact:** None

**Database migration required:** No

**Backwards compatibility:** Fully compatible — decorators wrap existing interfaces transparently.

## 6. Test Plan

### Unit Tests

- [ ] `[Unit] CacheService_GetListAsync_ReturnsCachedItems_WhenPresent` — verify cache hit
- [ ] `[Unit] CacheService_GetListAsync_ReturnsNull_WhenMiss` — verify cache miss
- [ ] `[Unit] CacheService_GetListAsync_ReturnsNull_OnRedisFailure` — verify resilience
- [ ] `[Unit] CacheService_SetListAsync_SerializesAndStores` — verify write
- [ ] `[Unit] CacheService_SetListAsync_LogsWarning_OnRedisFailure` — verify silent failure
- [ ] `[Unit] ResilientPublisher_PublishAsync_DelegatesToEndpoint` — verify delegation
- [ ] `[Unit] ResilientPublisher_PublishAsync_LogsWarning_OnFailure` — verify silent failure
- [ ] `[Unit] ResilientPublisher_PublishAsync_DoesNotThrow_OnFailure` — verify fire-and-forget
- [ ] `[Unit] ResilienceProfileRegistry_ReturnsStandardProfile` — verify default
- [ ] `[Unit] ResilienceProfileRegistry_ReturnsPermissiveProfile` — verify named profile

### Integration Tests

- [ ] `[Integration] CachedPermissionService_ServesFromCache_AfterFirstFetch` — verify caching behavior
- [ ] `[Integration] ResilientPublisher_PublishesEvent_WhenRabbitMQAvailable` — verify happy path

## 7. Detailed Design

### Cache Decorator

```csharp
// src/Warehouse.Infrastructure/Caching/ICacheService.cs
public interface ICacheService<T>
{
    Task<IReadOnlyList<T>?> GetListAsync(string key, CancellationToken cancellationToken);
    Task SetListAsync(string key, IReadOnlyList<T> items, TimeSpan ttl, CancellationToken cancellationToken);
    Task InvalidateAsync(string key, CancellationToken cancellationToken);
}

// src/Warehouse.Infrastructure/Caching/DistributedCacheService.cs
public sealed class DistributedCacheService<T> : ICacheService<T>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService<T>> _logger;

    public async Task<IReadOnlyList<T>?> GetListAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            byte[]? cached = await _cache.GetAsync(key, cancellationToken);
            return cached is null ? null : JsonSerializer.Deserialize<List<T>>(cached);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache read failed for key {CacheKey}", key);
            return null;
        }
    }
    // SetListAsync and InvalidateAsync follow same pattern
}
```

### Resilient Publisher

```csharp
// src/Warehouse.Infrastructure/Messaging/IResilientPublisher.cs
public interface IResilientPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : class;
}

// src/Warehouse.Infrastructure/Messaging/ResilientPublisher.cs
public sealed class ResilientPublisher : IResilientPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ResilientPublisher> _logger;

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : class
    {
        try
        {
            await _publishEndpoint.Publish(@event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish {EventType}", typeof(TEvent).Name);
        }
    }
}
```

### Resilience Profiles

```csharp
// src/Warehouse.Infrastructure/Resilience/ResilienceProfiles.cs
public static class ResilienceProfiles
{
    public const string Standard = "Standard";
    public const string Permissive = "Permissive";
    public const string Strict = "Strict";

    public static void Apply(string profileName, HttpStandardResilienceOptions options)
    {
        // Switch on profileName to configure retry, circuit breaker, timeouts
    }
}
```

### Services Affected

| Service | Current Pattern | New Pattern |
|---|---|---|
| `RoleService` | Private `GetCachedListAsync`/`SetCacheAsync` methods | Inject `ICacheService<RoleDto>` |
| `PermissionService` | Private cache methods | Inject `ICacheService<PermissionDto>` |
| `CustomerCategoryService` | Private cache methods | Inject `ICacheService<CustomerCategoryDto>` |
| `LocalUserPermissionService` | Private `TryGetFromCacheAsync`/`TrySetCacheAsync` | Inject `ICacheService<string>` |
| `UserService` | Try-catch around `_publishEndpoint.Publish` | Inject `IResilientPublisher` |
| `RoleService` | Try-catch around publish | Inject `IResilientPublisher` |
| `CustomerService` | Try-catch around publish | Inject `IResilientPublisher` |
| `AuditService` | Try-catch around publish | Inject `IResilientPublisher` |
| `StockMovementService` | Try-catch around publish | Inject `IResilientPublisher` |
| `InventoryAdjustmentService` | Try-catch around publish | Inject `IResilientPublisher` |
| `WarehouseTransferService` | Try-catch around publish | Inject `IResilientPublisher` |

### DI Registration

```csharp
// In ServiceCollectionExtensions
services.AddSingleton(typeof(ICacheService<>), typeof(DistributedCacheService<>));
services.AddScoped<IResilientPublisher, ResilientPublisher>();
```

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-AUTH-001 | No behavior change — cache and event logic wrapped in decorators |
| SDD-CUST-001 | No behavior change — cache and event logic wrapped in decorators |
| SDD-INV-002 | No behavior change — event publishing wrapped in decorator |
| SDD-INFRA-001 | Infrastructure section updated to document `ICacheService<T>` and `IResilientPublisher` |

## Migration Plan

1. **Pre-deployment:** Implement `ICacheService<T>` and `IResilientPublisher` in Warehouse.Infrastructure. Register in DI. Migrate one service (e.g., PermissionService) as proof of concept. Run all tests. Migrate remaining services.
2. **Deployment:** Deploy updated assemblies — no schema changes.
3. **Post-deployment:** Verify cached endpoints still serve data correctly; verify events are published to RabbitMQ.
4. **Rollback:** Revert to previous commit — no data migration needed.

## Open Questions

- [ ] Should `ICacheService<T>` support single-item caching (`GetAsync<T>(key)`) in addition to list caching?
- [ ] Should `IResilientPublisher` support batch publishing (`PublishAllAsync<TEvent>(IEnumerable<TEvent>)`)?
- [ ] Should resilience profiles be configurable via `appsettings.json` or hardcoded as constants?
