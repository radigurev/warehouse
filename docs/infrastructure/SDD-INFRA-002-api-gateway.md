# SDD-INFRA-002 — API Gateway

> Status: Implemented
> Last updated: 2026-04-07
> Owner: TBD
> Category: Infrastructure

## 1. Context & Scope

This spec defines the behavior of the `Warehouse.Gateway` YARP reverse proxy, which serves as the single entry point for all client traffic to the Warehouse backend microservices. The gateway handles path-based routing, per-IP rate limiting, downstream health aggregation, correlation ID propagation, and structured logging.

**ISA-95 Conformance:** Cross-cutting infrastructure -- not domain-specific. The gateway is the unified ingress for the Vue.js SPA frontend to reach all Level 3 (Operations Management) and Level 4 (Business Planning) backend services. It does not implement any ISA-95 activity model function directly.

**In scope:**
- YARP reverse proxy route configuration (path-based routing to backend clusters)
- Global and named rate limiting policies (ASP.NET Core RateLimiting)
- Downstream service health aggregation (`/health` endpoint)
- Correlation ID middleware (generate/propagate `X-Correlation-ID`)
- Structured logging via NLog with Loki integration
- OpenTelemetry tracing registration

**Out of scope:**
- JWT authentication and authorization (handled by each backend service individually; the gateway passes tokens through transparently)
- CORS configuration (the SPA is served from the same origin as the gateway)
- Request/response transformation (YARP forwards requests unmodified)
- Load balancing across multiple instances of a single service (single destination per cluster in current configuration)

**Related specs:**
- `SDD-AUTH-001` -- Authentication and Authorization (JWT validation occurs at backend services, not at the gateway)

---

## 2. Behavior

### 2.1 Reverse Proxy Routing

- The gateway MUST run on port 5000 (configurable via `Kestrel:Endpoints:Http:Url`).
- The gateway MUST use YARP 2.3.0 as the reverse proxy engine.
- Route configuration MUST be loaded from the `ReverseProxy` section of `appsettings.json` via `LoadFromConfig()`.
- The gateway MUST define three backend clusters:

| Cluster ID | Default Destination | Service |
|---|---|---|
| `auth-cluster` | `http://localhost:5001` | Warehouse.Auth.API |
| `customers-cluster` | `http://localhost:5002` | Warehouse.Customers.API |
| `inventory-cluster` | `http://localhost:5003` | Warehouse.Inventory.API |

- Each route MUST use a `{**catch-all}` path pattern to forward all sub-paths to the target cluster.
- The gateway MUST define 22 routes (current implementation):

**Auth cluster (5 routes):**

| Route ID | Path Pattern |
|---|---|
| `auth-route` | `/api/v1/auth/{**catch-all}` |
| `users-route` | `/api/v1/users/{**catch-all}` |
| `roles-route` | `/api/v1/roles/{**catch-all}` |
| `permissions-route` | `/api/v1/permissions/{**catch-all}` |
| `audit-route` | `/api/v1/audit/{**catch-all}` |

**Customers cluster (2 routes):**

| Route ID | Path Pattern |
|---|---|
| `customers-route` | `/api/v1/customers/{**catch-all}` |
| `customer-categories-route` | `/api/v1/customer-categories/{**catch-all}` |

**Inventory cluster (15 routes):**

| Route ID | Path Pattern |
|---|---|
| `products-route` | `/api/v1/products/{**catch-all}` |
| `product-categories-route` | `/api/v1/product-categories/{**catch-all}` |
| `units-of-measure-route` | `/api/v1/units-of-measure/{**catch-all}` |
| `product-accessories-route` | `/api/v1/product-accessories/{**catch-all}` |
| `product-substitutes-route` | `/api/v1/product-substitutes/{**catch-all}` |
| `bom-route` | `/api/v1/bom/{**catch-all}` |
| `warehouses-route` | `/api/v1/warehouses/{**catch-all}` |
| `zones-route` | `/api/v1/zones/{**catch-all}` |
| `storage-locations-route` | `/api/v1/storage-locations/{**catch-all}` |
| `stock-levels-route` | `/api/v1/stock-levels/{**catch-all}` |
| `stock-movements-route` | `/api/v1/stock-movements/{**catch-all}` |
| `batches-route` | `/api/v1/batches/{**catch-all}` |
| `adjustments-route` | `/api/v1/adjustments/{**catch-all}` |
| `transfers-route` | `/api/v1/transfers/{**catch-all}` |
| `stocktake-route` | `/api/v1/stocktake/{**catch-all}` |

**Edge cases:**
- A request to a path not matching any defined route MUST result in a 404 Not Found response from the gateway.
- When new microservices are added (e.g., Purchasing, Fulfillment), their routes MUST be added to the `ReverseProxy:Routes` configuration and a new cluster MUST be defined in `ReverseProxy:Clusters`.

### 2.2 JWT Passthrough

- The gateway MUST NOT perform JWT authentication or authorization. It MUST NOT register `UseAuthentication()` or `UseAuthorization()` middleware.
- The gateway MUST forward all HTTP headers (including `Authorization: Bearer <token>`) to the downstream service unmodified.
- JWT validation and permission checks MUST be the responsibility of each individual backend service (see `SDD-AUTH-001`).

### 2.3 Rate Limiting

- The gateway MUST register ASP.NET Core rate limiting middleware via `AddRateLimiter()`.
- When any rate limit is exceeded, the gateway MUST return HTTP 429 Too Many Requests (configured via `RejectionStatusCode`).

**Global limiter (applied to all requests):**

| Setting | Value |
|---|---|
| Type | Fixed window, partitioned by IP |
| Partition key | `HttpContext.Connection.RemoteIpAddress` (falls back to `"unknown"` if null) |
| Permit limit | 200 requests per window |
| Window | 1 minute |
| Queue limit | 20 requests |
| Queue processing order | Oldest first (FIFO) |

**Named policy `"fixed"` (available for specific route assignment):**

| Setting | Value |
|---|---|
| Type | Fixed window |
| Permit limit | 100 requests per window |
| Window | 1 minute |
| Queue limit | 10 requests |
| Queue processing order | Oldest first (FIFO) |

- The named `"fixed"` policy is currently not assigned to any specific route. It MAY be applied to individual routes in the future for stricter per-route limiting.

**Edge cases:**
- When the client IP address is not available (e.g., behind a misconfigured proxy), the global limiter MUST use the partition key `"unknown"`, effectively sharing the limit across all unidentifiable clients.
- After the 1-minute window resets, the client MUST be able to make requests again up to the permit limit.

### 2.4 Health Aggregation

- The gateway MUST expose a `/health` endpoint that aggregates the health status of all downstream services.
- The health endpoint MUST probe each downstream service's `/health/ready` endpoint using `AddUrlGroup()` URI health checks.
- The health check URLs MUST be configurable via the `HealthChecks:AuthApi`, `HealthChecks:CustomersApi`, and `HealthChecks:InventoryApi` configuration keys.
- Default health check URLs MUST be:

| Check Name | Default URL |
|---|---|
| `auth-api` | `http://localhost:5001/health/ready` |
| `customers-api` | `http://localhost:5002/health/ready` |
| `inventory-api` | `http://localhost:5003/health/ready` |

- All downstream health checks MUST be tagged with `"ready"`.
- If any downstream service is unreachable, the aggregated health check SHOULD report an unhealthy status.

**Edge case:**
- If a downstream service is temporarily unavailable, the `/health` endpoint MUST still respond (it MUST NOT hang indefinitely). The URI health check library applies its own default timeout.

### 2.5 Middleware Pipeline Order

The gateway middleware pipeline MUST be registered in the following order:

1. `CorrelationIdMiddleware` -- generates or propagates `X-Correlation-ID` header
2. `UseRateLimiter()` -- applies rate limiting before routing
3. `MapHealthChecks("/health")` -- health endpoint (not rate-limited by the global limiter on terminal middleware)
4. `MapReverseProxy()` -- YARP reverse proxy routing

- The gateway MUST NOT register `UseAuthentication()`, `UseAuthorization()`, or `UseCors()` middleware.

### 2.6 Correlation ID Propagation

- The gateway MUST use `CorrelationIdMiddleware` from `Warehouse.Infrastructure`.
- If the incoming request contains an `X-Correlation-ID` header, the gateway MUST use that value and propagate it to the downstream service.
- If the incoming request does not contain an `X-Correlation-ID` header, the gateway MUST generate a new GUID (formatted as `"D"`) and use it as the correlation ID.
- The correlation ID MUST be stored in `HttpContext.Items["CorrelationId"]`.
- The correlation ID MUST be added to the response headers as `X-Correlation-ID`.
- The correlation ID MUST be pushed to the NLog `ScopeContext` for inclusion in all log entries during the request.

### 2.7 Logging

- The gateway MUST use NLog via `UseNLog()` for structured logging.
- NLog MUST be configured with three targets:

| Target | Type | Description |
|---|---|---|
| `console` | Console | Standard output with correlation ID in layout |
| `file` | File | `logs/gateway-${shortdate}.log`, daily archive, 30 max archive files |
| `loki` | Loki | Push to Grafana Loki endpoint (default `http://localhost:3100`) |

- The Loki target MUST include the following labels: `service` = `warehouse-gateway`, `level`, `correlation_id`.
- All log layouts MUST include the correlation ID via `${scopeproperty:CorrelationId}`.
- `Microsoft.AspNetCore.*` loggers at Info level and below MUST be suppressed (final rule).
- `Yarp.*` loggers at Info level and above MUST be written to all three targets.

### 2.8 Distributed Tracing

- The gateway MUST register OpenTelemetry tracing via `AddWarehouseTracing()` with service name `"warehouse-gateway"`.
- The OTLP endpoint MUST be configurable via `OpenTelemetry:OtlpEndpoint` (default `http://localhost:4317`).

### 2.9 Startup and Shutdown

- The gateway MUST log `"Starting Warehouse.Gateway"` on startup at Info level.
- If startup fails with an unhandled exception, the gateway MUST log the exception at Error level with the message `"Gateway startup failed"` and re-throw.
- On shutdown, NLog MUST be flushed and shut down via `LogManager.Shutdown()` in a `finally` block.

---

## 3. Validation Rules

| # | Setting | Rule | Default | Error |
|---|---|---|---|---|
| V1 | `HealthChecks:AuthApi` | Optional. Must be a valid HTTP/HTTPS URL if provided. | `http://localhost:5001` | Health check fails if service at URL is unreachable |
| V2 | `HealthChecks:CustomersApi` | Optional. Must be a valid HTTP/HTTPS URL if provided. | `http://localhost:5002` | Health check fails if service at URL is unreachable |
| V3 | `HealthChecks:InventoryApi` | Optional. Must be a valid HTTP/HTTPS URL if provided. | `http://localhost:5003` | Health check fails if service at URL is unreachable |
| V4 | `OpenTelemetry:OtlpEndpoint` | Optional. Must be a valid HTTP/HTTPS URL if provided. | `http://localhost:4317` | Traces not exported if endpoint unreachable |
| V5 | `ReverseProxy` configuration section | MUST be present. MUST contain at least one route and one cluster. | See appsettings.json | Gateway starts but cannot route any requests |
| V6 | `Kestrel:Endpoints:Http:Url` | Optional. Must be a valid URL with port. | `http://localhost:5000` | Gateway fails to bind if port is in use |

---

## 4. Error Rules

| # | Condition | HTTP Status | Response Body | Notes |
|---|---|---|---|---|
| E1 | Rate limit exceeded (global: >200 req/min per IP) | 429 | Too Many Requests | ASP.NET Core default 429 response |
| E2 | Rate limit exceeded (named "fixed" policy: >100 req/min per IP) | 429 | Too Many Requests | Only when policy is assigned to a route |
| E3 | Request path does not match any defined route | 404 | Not Found | YARP returns 404 for unmatched paths |
| E4 | Downstream service unavailable or returns error | Passthrough | Passthrough | YARP forwards the downstream response as-is; if the service is completely unreachable, YARP returns 502 Bad Gateway |
| E5 | Downstream health check fails | N/A (health endpoint) | Unhealthy status in `/health` response | Reported via health check aggregation, not as an API error |

---

## 5. Configuration Keys

| Key | Default | Used By | Description |
|---|---|---|---|
| `Kestrel:Endpoints:Http:Url` | `http://localhost:5000` | Kestrel | Gateway listen address and port |
| `HealthChecks:AuthApi` | `http://localhost:5001` | Health checks | Auth service base URL for readiness probing |
| `HealthChecks:CustomersApi` | `http://localhost:5002` | Health checks | Customers service base URL for readiness probing |
| `HealthChecks:InventoryApi` | `http://localhost:5003` | Health checks | Inventory service base URL for readiness probing |
| `OpenTelemetry:OtlpEndpoint` | `http://localhost:4317` | OpenTelemetry | Jaeger OTLP collector endpoint |
| `ReverseProxy:Routes` | (22 routes) | YARP | Path-based route definitions |
| `ReverseProxy:Clusters` | (3 clusters) | YARP | Backend cluster destination addresses |

---

## 6. Versioning Notes

- **v1 -- Initial specification (2026-04-07)**
  - Documenting already-implemented YARP gateway behavior
  - 22 routes across 3 clusters (Auth, Customers, Inventory)
  - Global rate limiting (200 req/min per IP) and named "fixed" policy (100 req/min)
  - Downstream health aggregation via `/health`
  - Correlation ID propagation via shared `CorrelationIdMiddleware`
  - NLog structured logging with Console, File, and Loki targets
  - OpenTelemetry tracing with `warehouse-gateway` service name
  - JWT passthrough (no authentication at gateway level)

---

## 7. Test Plan

### Integration Tests -- GatewayRoutingTests

- `AuthRoute_ForwardsToAuthCluster` [Integration]
- `UsersRoute_ForwardsToAuthCluster` [Integration]
- `CustomersRoute_ForwardsToCustomersCluster` [Integration]
- `InventoryRoute_ForwardsToInventoryCluster` [Integration]
- `UnknownRoute_Returns404` [Integration]
- `AllDefinedRoutes_AreForwardedToCorrectCluster` [Integration]

### Integration Tests -- RateLimitingTests

- `Request_WithinGlobalLimit_Returns200` [Integration]
- `Request_ExceedsGlobalLimit_Returns429` [Integration]
- `Request_AfterWindowResets_AllowsRequestsAgain` [Integration]
- `Request_FromDifferentIPs_IndependentLimits` [Integration]

### Integration Tests -- HealthAggregationTests

- `HealthEndpoint_AllServicesHealthy_ReturnsHealthy` [Integration]
- `HealthEndpoint_ServiceUnreachable_ReturnsUnhealthy` [Integration]
- `HealthEndpoint_RespondsWithoutHanging` [Integration]

### Integration Tests -- CorrelationIdTests

- `Request_NoCorrelationId_GatewayGeneratesNewGuid` [Integration]
- `Request_WithCorrelationId_GatewayPreservesValue` [Integration]
- `Response_AlwaysIncludesCorrelationIdHeader` [Integration]

### Integration Tests -- JwtPassthroughTests

- `Request_WithAuthorizationHeader_ForwardedToDownstream` [Integration]
- `Request_WithoutAuthorizationHeader_StillForwarded` [Integration]

### Integration Tests -- MiddlewarePipelineTests

- `Pipeline_CorrelationIdSetBeforeRateLimiter` [Integration]
- `Pipeline_NoAuthenticationMiddlewareRegistered` [Integration]

---

## Key Files

- `src/Gateway/Warehouse.Gateway/Program.cs`
- `src/Gateway/Warehouse.Gateway/appsettings.json`
- `src/Gateway/Warehouse.Gateway/appsettings.json.template`
- `src/Gateway/Warehouse.Gateway/nlog.config`
- `src/Gateway/Warehouse.Gateway/Warehouse.Gateway.csproj`
- `src/Warehouse.Infrastructure/Middleware/CorrelationIdMiddleware.cs`
- `src/Warehouse.Infrastructure/Extensions/ServiceCollectionExtensions.cs` (provides `AddWarehouseTracing`)
