# SDD-INFRA-001 — Shared Infrastructure Library

> Status: Active
> Last updated: 2026-04-07
> Owner: TBD
> Category: Infrastructure

## 1. Context & Scope

This spec defines the cross-cutting infrastructure provided by the `Warehouse.Infrastructure` shared library. Every Warehouse microservice references this project to gain a consistent middleware pipeline, authentication, authorization, resilience, caching, messaging, health checks, and common base types. The library is domain-agnostic and MUST NOT contain business logic.

**ISA-95 Conformance:** ISA-95 Part 2 -- cross-cutting infrastructure supporting all operations domains. This library is not specific to any single ISA-95 operations domain; it provides the foundational plumbing that all domain services consume.

**In scope:**

- Middleware pipeline ordering and registration (`UseWarehousePipeline`)
- Correlation ID propagation (inbound, outbound, logging context)
- Global exception handling with ProblemDetails responses
- Permission-based authorization (`RequirePermissionAttribute`, handler, policy provider)
- Base API controller (`BaseApiController`) with Result-to-ActionResult helpers
- Base entity service (`BaseEntityService<TContext>`) with find/map/save helpers
- Primary flag management utility (`PrimaryFlagHelper`)
- JWT Bearer authentication registration and configuration
- API versioning (URL-based, `/api/v{version}/`)
- Swagger/OpenAPI with JWT security definition
- Health check registration (SQL Server, Redis readiness probes, liveness endpoint)
- Redis distributed cache registration (`IDistributedCache`)
- MassTransit/RabbitMQ message bus registration
- Polly resilience for typed HttpClients (retry, circuit breaker, timeouts)
- Feature flag registration (`IFeatureManager`)
- OpenTelemetry distributed tracing registration
- `Result` and `Result<T>` outcome types (defined in `Warehouse.Common`, consumed heavily here)

**Out of scope:**

- Domain-specific services, controllers, or business logic (see domain specs)
- NLog configuration files (defined per service, not in this library)
- Rate limiting (configured on the Gateway, not in this library)
- Database seeding (implemented per service)
- EF Core DbContext definitions (defined per domain in `*.DBModel` projects)
- Specific cache key definitions and TTLs (defined by consuming services)

**Related specs:**

- `SDD-AUTH-001` -- Authentication and authorization (consumes JWT middleware, permission handler, correlation ID)
- `SDD-CUST-001` -- Customers and accounts (consumes base controller, base entity service, cache, messaging)
- `SDD-INV-001` through `SDD-INV-004` -- Inventory domain (consumes all infrastructure components)
- `SDD-PURCH-001` -- Procurement operations (consumes base controller, base entity service, messaging)
- `SDD-FULF-001` -- Fulfillment operations (consumes base controller, base entity service, messaging)

---

## 2. Behavior

### 2.1 Middleware Pipeline Order

The `UseWarehousePipeline(swaggerEndpointTitle)` extension method on `WebApplication` MUST configure the middleware pipeline in the following exact order:

1. `CorrelationIdMiddleware` -- MUST be first in the pipeline
2. `GlobalExceptionHandlerMiddleware` -- MUST be second in the pipeline
3. Swagger UI -- MUST only be enabled in Development environment
4. `UseAuthentication()` -- JWT Bearer validation
5. `UseAuthorization()` -- permission-based checks
6. `MapControllers()` -- controller endpoint routing
7. Health check endpoints:
   - `/health/live` -- liveness probe with `Predicate = _ => false` (always healthy, no dependency checks)
   - `/health/ready` -- readiness probe filtered to checks tagged `"ready"` (SQL Server, Redis)

**Edge cases:**

- If `UseWarehousePipeline` is called in a non-Development environment, Swagger UI MUST NOT be registered.
- The liveness endpoint MUST always return HTTP 200 regardless of downstream dependency health.

### 2.2 Correlation ID Middleware

`CorrelationIdMiddleware` MUST:

- Read the `X-Correlation-ID` header from the incoming request.
- If the header is not present, generate a new GUID in `"D"` format (lowercase with hyphens).
- Store the correlation ID in `HttpContext.Items["CorrelationId"]`.
- Push the correlation ID into the NLog scope context as the `CorrelationId` property via `NLog.ScopeContext.PushProperty`.
- Register a callback via `Response.OnStarting` to set the `X-Correlation-ID` response header.
- Dispose the NLog scope property after the request completes (in a `finally` block).

The header name constant MUST be `"X-Correlation-ID"` and the items key constant MUST be `"CorrelationId"`, both defined as `public const` fields on the middleware class.

**Edge cases:**

- If the incoming `X-Correlation-ID` header contains a value, the middleware MUST use that value verbatim (no re-generation).
- If an exception is thrown by downstream middleware, the NLog scope MUST still be disposed (guaranteed by the `finally` block).

### 2.3 Correlation ID Delegating Handler

`CorrelationIdDelegatingHandler` MUST:

- Read the correlation ID from `HttpContext.Items["CorrelationId"]` via `IHttpContextAccessor`.
- If a correlation ID is present, add it as the `X-Correlation-ID` header on the outbound `HttpRequestMessage` using `TryAddWithoutValidation`.
- If no correlation ID is present (e.g., background task without HTTP context), the handler MUST NOT add the header and MUST NOT throw.

DI registration: `AddCorrelationId()` MUST register `IHttpContextAccessor` and `CorrelationIdDelegatingHandler` as transient.

### 2.4 Global Exception Handler Middleware

`GlobalExceptionHandlerMiddleware` MUST:

- Wrap the entire downstream pipeline in a `try/catch`.
- On unhandled exception, log the error via `ILogger` using a structured message template containing `{Method}` and `{Path}`.
- Return HTTP 500 with content type `application/problem+json`.
- Return a ProblemDetails body with:
  - `type`: `"https://warehouse.local/errors/INTERNAL_ERROR"`
  - `title`: `"Internal Server Error"`
  - `status`: `500`
  - `detail`: `"An unexpected error occurred. Please try again later."` (generic in all environments)
  - `instance`: the request path
- Serialize ProblemDetails using `System.Text.Json` with `camelCase` naming policy.

**Edge cases:**

- If no exception occurs, the middleware MUST pass the request through without modification.
- The detail field MUST NOT include the exception message or stack trace in any environment (prevent information leakage).

### 2.5 Permission-Based Authorization

The authorization system consists of four classes:

**`RequirePermissionAttribute`** MUST:

- Extend `AuthorizeAttribute` with policy `"Permission:{permission}"`.
- Be applicable to classes and methods with `AllowMultiple = true`.
- Expose a `Permission` property containing the raw permission string.

**`PermissionRequirement`** MUST:

- Implement `IAuthorizationRequirement`.
- Hold the required permission string (e.g., `"customers:read"`).

**`PermissionPolicyProvider`** MUST:

- Implement `IAuthorizationPolicyProvider`.
- For policy names starting with `"Permission:"`, dynamically create a policy requiring an authenticated user with the extracted `PermissionRequirement`.
- For all other policy names, delegate to the `DefaultAuthorizationPolicyProvider`.

**`PermissionAuthorizationHandler`** MUST:

- Extend `AuthorizationHandler<PermissionRequirement>`.
- Extract all `"permission"` claims from the JWT.
- Succeed the requirement if any permission claim matches:
  - **Exact match**: claim value equals the required permission (e.g., `"customers:read"` == `"customers:read"`).
  - **Wildcard match**: claim value equals `"{resource}:all"` for the same resource (e.g., `"customers:all"` grants `"customers:read"`).
- If no match is found, the handler MUST NOT call `context.Fail()` -- it simply does not call `context.Succeed()`, allowing other handlers to evaluate.

DI registration: `AddWarehouseAuthentication(configuration)` MUST register `PermissionPolicyProvider` and `PermissionAuthorizationHandler` as singletons.

**Edge cases:**

- A user with zero permission claims MUST fail all permission checks.
- A wildcard permission on a different resource (e.g., `"inventory:all"`) MUST NOT satisfy a requirement for `"customers:read"`.

### 2.6 Base API Controller

`BaseApiController` MUST be an abstract class decorated with `[ApiController]` and extending `ControllerBase`. It provides:

**`ToActionResult(Result result)`** MUST:

- Return `NoContent()` (204) when `result.IsSuccess` is true.
- Return a ProblemDetails `ObjectResult` with the appropriate status code when the result is a failure.

**`ToActionResult<T>(Result<T> result)`** MUST:

- Return `Ok(result.Value)` (200) when `result.IsSuccess` is true.
- Return a ProblemDetails `ObjectResult` with the appropriate status code when the result is a failure.

**`ToCreatedResult<T>(Result<T> result, string routeName, Func<T, object> routeValues)`** MUST:

- Return `CreatedAtRoute` (201) with the route name, route values derived from the value, and the value body when the result is a success.
- Return a ProblemDetails `ObjectResult` with the appropriate status code when the result is a failure.

**`GetIpAddress()`** MUST return the client IP from `HttpContext.Connection.RemoteIpAddress`.

**`GetCurrentUserId()`** MUST:

- Extract the user ID from the `ClaimTypes.NameIdentifier` claim, falling back to the `"sub"` claim.
- Return `int` (parsed from the claim value).

**`HasPermissionAsync(string permission, CancellationToken)`** MUST:

- Resolve `IAuthorizationService` from `HttpContext.RequestServices`.
- Authorize the current user against a `PermissionRequirement` for the given permission.
- Return `true` if authorization succeeds, `false` otherwise.

The private `ToProblemResult` method MUST produce a ProblemDetails with:

- `type`: `"https://warehouse.local/errors/{errorCode}"`
- `title`: the error code
- `status`: the status code from the Result
- `detail`: the error message from the Result
- `instance`: the request path

**Edge cases:**

- `GetCurrentUserId()` called without an authenticated user (no `sub` claim) will throw -- callers MUST ensure authentication is enforced via `[Authorize]` or `[RequirePermission]`.
- `GetIpAddress()` MAY return null if the connection has no remote IP (e.g., Unix socket).

### 2.7 Base Entity Service

`BaseEntityService<TContext>` where `TContext : DbContext` MUST be an abstract class providing:

**Constructor** MUST accept `TContext context` and `IMapper mapper`, exposing them as `protected` properties `Context` and `Mapper`.

**`FindOrNotFoundAsync<TEntity, TDto>(DbSet<TEntity>, int id, string errorCode, string errorMessage, CancellationToken)`** MUST:

- Find the entity by ID using `DbSet.FindAsync`.
- If found, map to `TDto` via AutoMapper and return `Result<TDto>.Success`.
- If not found, return `Result<TDto>.Failure(errorCode, errorMessage, 404)`.
- The entity type MUST implement `IEntity`.

**`MapToResult<TEntity, TDto>(TEntity entity)`** MUST map the entity to a DTO and return `Result<TDto>.Success`.

**`MapListToResult<TEntity, TDto>(IEnumerable<TEntity>)`** MUST map the collection to `IReadOnlyList<TDto>` and return the success result.

**`SaveChangesAsync(CancellationToken)`** MUST delegate to `Context.SaveChangesAsync`.

All async methods MUST use `ConfigureAwait(false)`.

**Edge cases:**

- `FindOrNotFoundAsync` with an ID that does not exist MUST return a 404 failure Result, not throw.
- `MapListToResult` with an empty collection MUST return a success Result containing an empty list.

### 2.8 Primary Flag Helper

`PrimaryFlagHelper` MUST be a static class providing:

**`UnsetOthersAsync<TEntity>(DbSet<TEntity>, Expression<Func<TEntity, bool>> filter, int excludeId, Action<TEntity> clearFlag, CancellationToken)`** MUST:

- Query all entities matching the filter except the one with `excludeId`.
- Call `clearFlag` on each matched entity to clear the primary/default flag.
- The entity type MUST implement `IEntity`.

**`PromoteNextAsync<TEntity>(DbSet<TEntity>, Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, DateTime>> orderBy, Action<TEntity> setFlag, CancellationToken)`** MUST:

- Query the first entity matching the filter ordered by the `orderBy` expression.
- If an entity is found, call `setFlag` to promote it.
- If no entity is found, do nothing (no-op).

**Edge cases:**

- `UnsetOthersAsync` when no other entities match the filter MUST complete without error.
- `PromoteNextAsync` when the DbSet has no matching entities MUST complete without error and not call `setFlag`.

### 2.9 Health Checks

`AddWarehouseHealthChecks(configuration)` MUST:

- Register a SQL Server health check using `ConnectionStrings:WarehouseDb`, tagged `["ready"]`.

`AddWarehouseRedisCache(configuration)` MUST also add a Redis health check tagged `["ready"]`.

Health check endpoints (registered in `UseWarehousePipeline`):

- `/health/live` -- MUST return HTTP 200 with no dependency checks (Predicate always returns false).
- `/health/ready` -- MUST run all checks tagged `"ready"` and return HTTP 200 only if all pass.

**Edge cases:**

- If SQL Server is unreachable, `/health/ready` MUST return HTTP 503 while `/health/live` still returns HTTP 200.
- If Redis is unreachable, `/health/ready` MUST return HTTP 503 while `/health/live` still returns HTTP 200.

### 2.10 Distributed Cache (Redis)

`AddWarehouseRedisCache(configuration)` MUST:

- Read `ConnectionStrings:Redis` from configuration, defaulting to `"localhost:6379"` if not present.
- Register `StackExchangeRedisCache` as the `IDistributedCache` implementation with instance name prefix `"warehouse:"`.
- Add a Redis readiness health check tagged `["ready"]`.

**Cache usage pattern** (enforced by consuming services, documented here as the contract):

- Cache key convention: `{service}:{entity}:all` (e.g., `auth:permissions:all`).
- On read: check cache first via `IDistributedCache.GetAsync`, deserialize from JSON if found.
- On cache miss: query the database, serialize to JSON, store with `AbsoluteExpirationRelativeToNow`.
- On write (create/update/delete): invalidate cache via `IDistributedCache.RemoveAsync`.
- Transactional data (stock levels, movements) MUST NOT be cached.

**Edge cases:**

- If Redis is unavailable, `IDistributedCache` operations SHOULD throw exceptions. Consuming services SHOULD handle these gracefully and fall back to database queries.
- The instance name prefix `"warehouse:"` ensures key isolation if Redis is shared with other applications.

### 2.11 Event Messaging (MassTransit/RabbitMQ)

`AddWarehouseMessageBus(configuration, configureConsumers?)` MUST:

- Read `RabbitMQ:Host` (default: `"localhost"`), `RabbitMQ:Username` (default: `"warehouse"`), `RabbitMQ:Password` (default: `"warehouse"`) from configuration.
- Register MassTransit with RabbitMQ transport using the default virtual host `"/"`.
- Call `cfg.ConfigureEndpoints(context)` for automatic consumer endpoint registration.
- If `configureConsumers` is provided, invoke it to register consumer types.

**Event publishing pattern** (enforced by consuming services):

- Inject `IPublishEndpoint` into services that publish domain events.
- Publish events after `SaveChangesAsync` succeeds.
- Wrap `_publishEndpoint.Publish(...)` in `try/catch` with an empty catch or logged warning -- RabbitMQ unavailability MUST NOT break the main operation (fire-and-forget).
- Event contracts MUST be `sealed record` types with `required` properties, defined in `Warehouse.ServiceModel/Events/`.
- Event naming convention: `{Entity}{PastTenseVerb}Event` (e.g., `StockMovementRecordedEvent`).

**Testing:** Test projects MUST use `services.AddMassTransitTestHarness()` to replace the real RabbitMQ transport with an in-memory bus.

**Edge cases:**

- If RabbitMQ is unreachable at startup, MassTransit will retry connections in the background. The service MUST start and serve HTTP requests regardless.
- If `configureConsumers` is null, no consumers are registered (publish-only mode).

### 2.12 Resilience (Polly)

`AddWarehouseHttpClient<TClient, TImpl>(baseAddress)` MUST:

- Register a typed `HttpClient` with the given base address.
- Attach `CorrelationIdDelegatingHandler` for correlation ID propagation to outbound requests.
- Configure the standard resilience handler with:
  - **Retry:** 3 maximum attempts, 500ms base delay, exponential backoff with jitter.
  - **Circuit breaker:** 30s sampling duration, 5 minimum throughput, 15s break duration.
  - **Attempt timeout:** 10 seconds per attempt.
  - **Total request timeout:** 30 seconds.
- The type constraints MUST require `TClient : class` and `TImplementation : class, TClient`.

**Edge cases:**

- If the circuit breaker is open, requests MUST fail immediately with a `BrokenCircuitException` without contacting the downstream service.
- Currently no consumers exist in the codebase. First use case is Phase 2 inter-service HTTP calls.

### 2.13 Feature Flags

`AddWarehouseFeatureFlags()` MUST:

- Register `IFeatureManager` from the `FeatureManagement` configuration section.

Feature flag constants MUST be defined in `FeatureFlags.cs`:

| Constant | Value | Purpose |
|---|---|---|
| `EnableDatabaseSeeding` | `"EnableDatabaseSeeding"` | Gates entire `DatabaseSeeder.SeedAsync()` |
| `EnableSeedDefaultAdmin` | `"EnableSeedDefaultAdmin"` | Gates default admin user creation |

- Feature flags MUST default to `false` if not present in configuration.
- New flags MUST be added as `public const string` fields in `FeatureFlags.cs` -- magic strings MUST NOT be used.
- Template config files MUST include explicit `true` or `false` for each flag.

**Edge cases:**

- If the `FeatureManagement` section is missing entirely, all flags evaluate to `false`.
- If a flag name is misspelled in code (not using the constant), the behavior defaults to `false` silently.

### 2.14 JWT Authentication

`AddWarehouseAuthentication(configuration)` MUST:

- Bind `JwtSettings` from the `"Jwt"` configuration section.
- Register JWT Bearer as the default authentication and challenge scheme.
- Configure `TokenValidationParameters` with:
  - `ValidateIssuer = true`
  - `ValidateAudience = true`
  - `ValidateLifetime = true`
  - `ValidateIssuerSigningKey = true`
  - `ValidIssuer` from `Jwt:Issuer`
  - `ValidAudience` from `Jwt:Audience`
  - `IssuerSigningKey` from `Jwt:SecretKey` (HMAC-SHA256 symmetric key)
  - `ClockSkew = TimeSpan.Zero` (no tolerance for token expiration)

**Edge cases:**

- If `Jwt:SecretKey` is null or missing, the service will fail to start (null reference on key construction).
- If the secret key is shorter than 256 bits (32 bytes), HMAC-SHA256 may still function but SHOULD be at least 32 characters for security.

### 2.15 API Versioning

`AddWarehouseApiVersioning()` MUST:

- Set default API version to `1.0`.
- Assume default version when unspecified (`AssumeDefaultVersionWhenUnspecified = true`).
- Report API versions in response headers (`ReportApiVersions = true`).
- Use `UrlSegmentApiVersionReader` for URL-based versioning (`/api/v{version}/`).
- Configure the API explorer with `GroupNameFormat = "'v'VVV"` and `SubstituteApiVersionInUrl = true`.

### 2.16 Swagger/OpenAPI

`AddWarehouseSwagger(title, description)` MUST:

- Register a Swagger document for version `"v1"` with the provided title and description.
- Add a JWT Bearer security definition named `"Bearer"` (HTTP scheme, bearer format JWT, in header).
- Add a global security requirement referencing the Bearer scheme on all endpoints.

Swagger UI MUST only be enabled in Development environment (enforced by `UseWarehousePipeline`).

### 2.17 OpenTelemetry Distributed Tracing

`AddWarehouseTracing(configuration, serviceName)` MUST:

- Read `OpenTelemetry:OtlpEndpoint` from configuration, defaulting to `"http://localhost:4317"`.
- Configure the resource with the given service name and version `"1.0.0"`.
- Enable auto-instrumentation for:
  - ASP.NET Core (with filter excluding `/health` paths)
  - HttpClient
  - SQL Client (with `SetDbStatementForText = true`)
- Export traces via OTLP to the configured endpoint.

**Edge cases:**

- If the OTLP endpoint is unreachable, trace export fails silently -- the service MUST continue operating normally.
- Health check requests (`/health/*`) MUST be filtered out from tracing to reduce noise.

---

## 3. Validation Rules

### 3.1 Configuration Validation

| # | Setting | Rule | Consequence |
|---|---|---|---|
| V1 | `Jwt:SecretKey` | Required. SHOULD be at least 32 characters. | App fails to start with null reference if missing. |
| V2 | `Jwt:Issuer` | Required. | App fails to start if missing. |
| V3 | `Jwt:Audience` | Required. | App fails to start if missing. |
| V4 | `ConnectionStrings:WarehouseDb` | Required for health checks. | SQL Server health check reports unhealthy. |
| V5 | `ConnectionStrings:Redis` | Optional. Defaults to `"localhost:6379"`. | Cache operations fail; services fall back to database. |
| V6 | `RabbitMQ:Host` | Optional. Defaults to `"localhost"`. | Events silently dropped (fire-and-forget pattern). |
| V7 | `RabbitMQ:Username` | Optional. Defaults to `"warehouse"`. | Connection fails if credentials are wrong. |
| V8 | `RabbitMQ:Password` | Optional. Defaults to `"warehouse"`. | Connection fails if credentials are wrong. |
| V9 | `OpenTelemetry:OtlpEndpoint` | Optional. Defaults to `"http://localhost:4317"`. | Traces not exported. |
| V10 | `FeatureManagement:*` | Optional section. Flags default to `false` if absent. | Feature-gated code paths are disabled. |

### 3.2 Type Constraints

| # | Rule | Error |
|---|---|---|
| V11 | `BaseEntityService<TContext>` requires `TContext : DbContext`. | Compile-time error. |
| V12 | `FindOrNotFoundAsync` requires `TEntity : class, IEntity`. | Compile-time error. |
| V13 | `PrimaryFlagHelper` methods require `TEntity : class, IEntity`. | Compile-time error. |
| V14 | `AddWarehouseHttpClient<TClient, TImpl>` requires `TClient : class` and `TImplementation : class, TClient`. | Compile-time error. |
| V15 | Feature flag names MUST use constants from `FeatureFlags.cs`. | Misnamed flags silently default to `false`. |

---

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | Unhandled exception caught by `GlobalExceptionHandlerMiddleware` | 500 | `INTERNAL_ERROR` | An unexpected error occurred. Please try again later. |
| E2 | Missing or invalid JWT token | 401 | *(JWT Bearer default)* | *(Framework default challenge response)* |
| E3 | Valid JWT but insufficient permissions (`PermissionAuthorizationHandler` fails) | 403 | *(Framework default forbid response)* | *(Framework default forbid response)* |
| E4 | Entity not found via `BaseEntityService.FindOrNotFoundAsync` | 404 | *(caller-defined)* | *(caller-defined)* |
| E5 | Redis unavailable | -- | -- | Falls back to database. No error exposed to client. |
| E6 | RabbitMQ unavailable during event publish | -- | -- | Event silently dropped. No error exposed to client. |
| E7 | OTLP endpoint unavailable | -- | -- | Traces not exported. No error exposed to client. |
| E8 | `Result` failure mapped by `BaseApiController.ToProblemResult` | *(from Result)* | *(from Result)* | ProblemDetails with type `https://warehouse.local/errors/{errorCode}` |

All error responses produced by `BaseApiController` and `GlobalExceptionHandlerMiddleware` MUST use ProblemDetails (RFC 7807) format with `application/problem+json` content type.

---

## 5. Configuration Keys

| Key | Used By | Default | Description |
|---|---|---|---|
| `Jwt:SecretKey` | `AddWarehouseAuthentication` | *(required)* | HMAC-SHA256 signing key |
| `Jwt:Issuer` | `AddWarehouseAuthentication` | *(required)* | JWT token issuer |
| `Jwt:Audience` | `AddWarehouseAuthentication` | *(required)* | JWT token audience |
| `Jwt:AccessTokenExpirationMinutes` | `JwtSettings` | `30` | Access token lifetime |
| `Jwt:RefreshTokenExpirationDays` | `JwtSettings` | `7` | Refresh token lifetime |
| `ConnectionStrings:WarehouseDb` | `AddWarehouseHealthChecks` | *(required)* | SQL Server connection string |
| `ConnectionStrings:Redis` | `AddWarehouseRedisCache` | `localhost:6379` | Redis connection string |
| `RabbitMQ:Host` | `AddWarehouseMessageBus` | `localhost` | RabbitMQ host |
| `RabbitMQ:Username` | `AddWarehouseMessageBus` | `warehouse` | RabbitMQ username |
| `RabbitMQ:Password` | `AddWarehouseMessageBus` | `warehouse` | RabbitMQ password |
| `OpenTelemetry:OtlpEndpoint` | `AddWarehouseTracing` | `http://localhost:4317` | Jaeger OTLP endpoint |
| `FeatureManagement:EnableDatabaseSeeding` | `IFeatureManager` | `false` | Gates database seeder |
| `FeatureManagement:EnableSeedDefaultAdmin` | `IFeatureManager` | `false` | Gates admin user seed |

---

## 6. Versioning Notes

- **v1 -- Initial specification (2026-04-07)**
  - Documents all existing `Warehouse.Infrastructure` components as implemented
  - Covers middleware pipeline, correlation ID, global exception handler, permission authorization, base controller, base entity service, primary flag helper, health checks, Redis cache, MassTransit messaging, Polly resilience, feature flags, JWT authentication, API versioning, Swagger, and OpenTelemetry tracing
  - Status: Active (reflects already-implemented behavior)

---

## 7. Test Plan

### Unit Tests -- CorrelationIdMiddlewareTests

- `InvokeAsync_NoHeader_GeneratesNewGuid` [Unit]
- `InvokeAsync_ExistingHeader_UsesProvidedValue` [Unit]
- `InvokeAsync_SetsResponseHeader` [Unit]
- `InvokeAsync_StoresInHttpContextItems` [Unit]

### Unit Tests -- GlobalExceptionHandlerMiddlewareTests

- `InvokeAsync_NoException_PassesThrough` [Unit]
- `InvokeAsync_UnhandledException_Returns500` [Unit]
- `InvokeAsync_UnhandledException_ReturnsProblemDetails` [Unit]
- `InvokeAsync_SetsApplicationProblemJsonContentType` [Unit]

### Unit Tests -- PermissionAuthorizationHandlerTests

- `HandleAsync_ExactPermissionMatch_Succeeds` [Unit]
- `HandleAsync_WildcardPermission_Succeeds` [Unit]
- `HandleAsync_NoMatchingPermission_Fails` [Unit]
- `HandleAsync_NoPermissionClaims_Fails` [Unit]
- `HandleAsync_DifferentResourceWildcard_Fails` [Unit]

### Unit Tests -- BaseApiControllerTests

- `ToActionResult_SuccessResult_ReturnsNoContent` [Unit]
- `ToActionResult_FailureResult_ReturnsProblemDetails` [Unit]
- `ToActionResultT_SuccessResult_ReturnsOkWithValue` [Unit]
- `ToActionResultT_FailureResult_ReturnsProblemDetails` [Unit]
- `ToCreatedResult_SuccessResult_ReturnsCreatedAtRoute` [Unit]
- `ToCreatedResult_FailureResult_ReturnsProblemDetails` [Unit]
- `GetCurrentUserId_ValidSubClaim_ReturnsUserId` [Unit]
- `GetCurrentUserId_NoClaim_Throws` [Unit]

### Unit Tests -- BaseEntityServiceTests

- `FindOrNotFoundAsync_EntityExists_ReturnsSuccessWithDto` [Unit]
- `FindOrNotFoundAsync_EntityNotFound_ReturnsFailure404` [Unit]
- `MapToResult_ValidEntity_ReturnsSuccessWithDto` [Unit]
- `MapListToResult_EmptyCollection_ReturnsSuccessWithEmptyList` [Unit]
- `MapListToResult_MultipleEntities_ReturnsSuccessWithDtoList` [Unit]

### Unit Tests -- PrimaryFlagHelperTests

- `UnsetOthersAsync_ExistingPrimary_UnsetsAllExceptExcluded` [Unit]
- `UnsetOthersAsync_NoOtherEntities_CompletesWithoutError` [Unit]
- `PromoteNextAsync_OtherEntitiesExist_PromotesFirstByOrder` [Unit]
- `PromoteNextAsync_NoEntities_DoesNothing` [Unit]

### Unit Tests -- CorrelationIdDelegatingHandlerTests

- `SendAsync_CorrelationIdInContext_AddsHeaderToOutboundRequest` [Unit]
- `SendAsync_NoCorrelationId_DoesNotAddHeader` [Unit]

---

## Key Files

| File | Purpose |
|---|---|
| `src/Warehouse.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | All `Add*` DI registration extension methods |
| `src/Warehouse.Infrastructure/Extensions/WebApplicationExtensions.cs` | `UseWarehousePipeline` middleware pipeline |
| `src/Warehouse.Infrastructure/Middleware/CorrelationIdMiddleware.cs` | Inbound correlation ID handling |
| `src/Warehouse.Infrastructure/Middleware/GlobalExceptionHandlerMiddleware.cs` | Global exception-to-ProblemDetails handler |
| `src/Warehouse.Infrastructure/Http/CorrelationIdDelegatingHandler.cs` | Outbound correlation ID propagation |
| `src/Warehouse.Infrastructure/Authorization/RequirePermissionAttribute.cs` | Permission attribute for controllers |
| `src/Warehouse.Infrastructure/Authorization/PermissionRequirement.cs` | Authorization requirement value object |
| `src/Warehouse.Infrastructure/Authorization/PermissionPolicyProvider.cs` | Dynamic policy provider for permissions |
| `src/Warehouse.Infrastructure/Authorization/PermissionAuthorizationHandler.cs` | JWT permission claim evaluator |
| `src/Warehouse.Infrastructure/Controllers/BaseApiController.cs` | Abstract base controller with Result helpers |
| `src/Warehouse.Infrastructure/Services/BaseEntityService.cs` | Abstract base service with EF Core helpers |
| `src/Warehouse.Infrastructure/Services/PrimaryFlagHelper.cs` | Static utility for IsPrimary/IsDefault management |
| `src/Warehouse.Infrastructure/Configuration/FeatureFlags.cs` | Feature flag name constants |
| `src/Warehouse.Infrastructure/Configuration/JwtSettings.cs` | Strongly-typed JWT configuration model |
| `src/Warehouse.Common/Models/Result.cs` | Non-generic Result outcome type |
| `src/Warehouse.Common/Models/ResultT.cs` | Generic Result<T> outcome type |
| `src/Warehouse.Common/Interfaces/IEntity.cs` | Entity marker interface with int Id |
