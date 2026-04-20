# CHG-FIX-001 — Forward bearer token in IUserPermissionService HttpClient

> Status: Implemented
> Last updated: 2026-04-20
> Owner: TBD
> Priority: P0

## 1. Context & Scope

**Problem.** After CHG-ENH-004 introduced server-side permission resolution via `IUserPermissionService` (Redis cache + HTTP fallback to Auth.API), every microservice that depends on `[RequirePermission]` started returning **403 Forbidden** the moment the Redis cache was cold. Cause: the typed `HttpClient` registered in `AddWarehousePermissionValidation` calls `auth-api/api/v1/users/{id}/permissions` **without an `Authorization` header**, so Auth.API's own `[Authorize]` requirement responds **401**, the resolver catches the resulting failure, returns an empty permission set, and `PermissionAuthorizationHandler` fails closed.

This was masked in normal use because Redis-cached entries from prior sessions kept the path warm. The bug surfaces on:
- A fresh container start.
- A Redis flush / TTL expiry.
- The first request from any user whose permissions have never been cached.

**Why this is a P0 fix.** It silently breaks every protected endpoint in every non-Auth service. The CHG-FEAT-007 product-prices endpoints surfaced the issue end-to-end, but the bug existed branch-wide.

**Scope:**

- [x] Backend code change (`Warehouse.Infrastructure`)
- [ ] Database changes
- [ ] Frontend changes
- [ ] Configuration changes

**In scope:**

- New `BearerTokenForwardingHandler : DelegatingHandler` in `Warehouse.Infrastructure/Authorization/`.
- DI registration changes in `AddWarehousePermissionValidation` (`Warehouse.Infrastructure/Extensions/ServiceCollectionExtensions.cs`).
- `IHttpContextAccessor` registration (added defensively — most services already had it indirectly via ASP.NET Core, but the extension method now guarantees it).

**Explicitly excluded:**

- Any change to the `IUserPermissionService` interface or its consumer surface.
- Any change to Auth.API's permission endpoint contract.
- Caching policy changes (TTL, key format) — out of scope.
- Refactoring `UserPermissionService` itself.

**Related specs:**

- `CHG-ENH-004` — original change that introduced the server-side resolution path; this fix closes the gap left in its inter-service HTTP wiring.
- `CHG-FEAT-007` — surfaced the bug while verifying the new product-prices endpoints end-to-end.
- `SDD-AUTH-001` — Authentication and Authorization (no spec change, behaviour now matches the spec for cold-cache scenarios).

---

## 2. Behavior (RFC 2119)

- The `IUserPermissionService` typed `HttpClient` MUST attach an `Authorization` header to every outbound request when an inbound `HttpContext` has one.
- The header value MUST be copied verbatim from `HttpContext.Request.Headers["Authorization"]` (preserving the `Bearer` scheme and the JWT exactly as the upstream caller supplied it).
- If the inbound request has no `Authorization` header (e.g., the resolver is invoked outside an HTTP request — background job, hosted service), the handler MUST NOT add any header and MUST NOT throw. The downstream call will go anonymous and fail upstream the same way it did before this fix.
- If the outbound request already has an `Authorization` header (e.g., the caller built one explicitly), the handler MUST NOT overwrite it.
- The handler MUST be inserted into the typed `HttpClient` pipeline **before** the resilience handler so retries observe the propagated token.

---

## 3. Validation Rules

Not applicable — this is an internal infrastructure fix with no user-facing inputs.

---

## 4. Error Rules

- The handler itself does not raise errors. Behaviour failures (Auth.API still returns 401/403, Redis unavailable, etc.) are surfaced by the existing `UserPermissionService.TryFetchFromAuthApiAsync` warning log and the existing fail-closed deny. This fix does not change that contract.

---

## 5. Versioning Notes

**API version impact:** None. No public API surface change.

**Database migration required:** No.

**Backwards compatibility:** Pure additive. Services that were already working (warm Redis cache) keep working; services that were failing (cold cache) now succeed. No consumer code change required.

**Versioning entries:**

- **v1 — Initial fix (2026-04-20).** Introduces `BearerTokenForwardingHandler`, registers `IHttpContextAccessor` defensively, wires the handler into the `IUserPermissionService` typed HttpClient pipeline.

---

## 6. Test Plan

### Manual verification (performed)

- [x] Cold-cache verification: `redis-cli FLUSHDB`, fresh login, hit `GET /api/v1/product-prices?pageSize=3` via gateway → **200 OK** with real data (was 403 before the fix).
- [x] `GET /api/v1/product-prices/resolve?productId=1&currencyCode=USD` via gateway → **200 OK** with the seeded price.
- [x] Existing `Warehouse.Infrastructure.Tests` UserPermissionService unit tests: 7/7 pass.

### Recommended automated test (follow-up)

- [ ] [Integration] `BearerTokenForwardingHandler_PropagatesAuthorizationHeader` — spin up a fake downstream and assert the outbound request carries the inbound token.
- [ ] [Integration] `BearerTokenForwardingHandler_NoInboundHeader_OmitsHeader` — same harness, no inbound `Authorization` → outbound has none.
- [ ] [Integration] `BearerTokenForwardingHandler_PreservesExistingHeader` — outbound request already has an `Authorization` set → handler leaves it alone.

These tests are not blocking the fix and can be added in a follow-up.

---

## 7. Detailed Design

### New file

`src/Warehouse.Infrastructure/Authorization/BearerTokenForwardingHandler.cs`

```csharp
public sealed class BearerTokenForwardingHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Headers.Authorization is null)
        {
            string? auth = accessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(auth))
                request.Headers.TryAddWithoutValidation("Authorization", auth);
        }
        return base.SendAsync(request, ct);
    }
}
```

### Modified file

`src/Warehouse.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — `AddWarehousePermissionValidation`:

```csharp
services.AddCorrelationId();
services.AddHttpContextAccessor();
services.AddTransient<BearerTokenForwardingHandler>();

services.AddHttpClient<IUserPermissionService, UserPermissionService>(client =>
    {
        client.BaseAddress = new Uri(authApiBaseAddress);
    })
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddHttpMessageHandler<BearerTokenForwardingHandler>()
    .AddStandardResilienceHandler(...)
```

The handler is registered **after** the correlation-ID handler so the outbound request carries both `X-Correlation-ID` and `Authorization`, and **before** the resilience handler so retries see the same token.

---

## Affected System Specs

| Spec ID       | Impact                                                                                                                           |
| ------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| `SDD-AUTH-001` | **No spec text change.** Behaviour now matches the spec on cold-cache. A note may be added under §2.5 referencing this fix.      |

---

## Migration Plan

1. **Pre-deployment:** none — pure code change, no migration.
2. **Deployment:** rebuild and restart every microservice that calls `AddWarehousePermissionValidation` (currently Customers, Inventory, Fulfillment, Purchasing, Nomenclature, EventLog).
3. **Post-deployment:** flush Redis (`redis-cli FLUSHDB`) to force cache repopulation through the fixed code path; smoke-test one protected endpoint per service.
4. **Rollback:** revert the commit. Cold-cache behaviour returns to fail-closed 403 — no data corruption.

---

## Open Questions

- [ ] Should the resolver also forward `X-Correlation-ID` explicitly via this handler? (Already handled by `CorrelationIdDelegatingHandler`, kept separate by intent.)
- [ ] When inter-service-as-a-service-account auth arrives, the bearer-forwarding handler should be paired with (or replaced by) a service-to-service token handler. Out of scope for this fix.
