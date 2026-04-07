# SDD-OBS-001 — Observability (Centralized Logging & Distributed Tracing)

> Status: Active
> Last updated: 2026-04-07
> Owner: TBD
> Category: Infrastructure

## 1. Context & Scope

This spec defines the observability infrastructure for all Warehouse microservices. It covers centralized log aggregation via NLog, Loki, and Grafana, and distributed tracing via OpenTelemetry and Jaeger. Together these subsystems provide operations visibility across all ISA-95 Level 3 services without affecting request processing.

**ISA-95 Conformance:** Cross-cutting infrastructure supporting ISA-95 Part 3 operations event tracking. Observability provides the technical foundation for monitoring operations across all ISA-95 domains (Inventory, Personnel, Procurement, Fulfillment).

**In scope:**
- NLog configuration (console, file, and Loki targets) across all services
- Loki log aggregation and Grafana log visualization
- OpenTelemetry distributed tracing with ASP.NET Core, HttpClient, and SQL Client instrumentation
- Jaeger trace collection and UI
- Correlation ID integration with logging (provided by CorrelationIdMiddleware)
- Docker infrastructure containers for Loki, Grafana, and Jaeger
- Log level filtering rules per service
- Service naming conventions for logging and tracing

**Out of scope:**
- Correlation ID middleware implementation (documented separately — cross-cutting infrastructure)
- Health check endpoints (documented separately — cross-cutting infrastructure)
- Application-level metrics and dashboards (future enhancement)
- Alerting rules and notification channels (future enhancement)
- Log-based monitoring and anomaly detection (future enhancement)
- Redis, RabbitMQ, or SQL Server infrastructure (documented separately)

**Related specs:**
- `SDD-AUTH-001` — Auth service uses NLog for authentication event logging (login, logout, refresh)

---

## 2. Behavior

### 2.1 Centralized Logging Architecture

The system MUST use NLog (via `NLog.Web.AspNetCore` 5.4.0) as the logging framework for all microservices and the API gateway.

Each service MUST configure exactly three NLog targets:

1. **Console target** — stdout output for local development and Docker log collection
2. **File target** — per-service log file with daily rotation and 30-day archive retention
3. **Loki target** — centralized log aggregation via HTTP push (using `NLog.Targets.Loki` 2.5.0)

All three targets MUST use the same log layout format:

```
${longdate}|${level:uppercase=true}|${logger}|${scopeproperty:CorrelationId}|${message}${onexception:inner=${newline}${exception:format=tostring}}
```

The system MUST include the `CorrelationId` scope property in all log entries. This value is set by the `CorrelationIdMiddleware` via `NLog.ScopeContext.PushProperty`.

**Edge cases:**
- If the `CorrelationId` scope property is not set (e.g., background tasks outside a request context), the field MUST render as empty — it MUST NOT cause a logging failure.
- If NLog configuration is invalid (`throwConfigExceptions="true"`), the service MUST fail fast on startup. NLog internal errors MUST be written to `${basedir}/logs/nlog-internal.log`.

### 2.2 NLog File Target Configuration

Each service MUST configure a file target with the following settings:

| Setting | Value | Rationale |
|---|---|---|
| `fileName` | `${basedir}/logs/{service-log-name}-${shortdate}.log` | One file per day per service |
| `archiveEvery` | `Day` | Daily file rotation |
| `maxArchiveFiles` | `30` | 30-day retention window |

The file log name MUST match the service identity:

| Service | File Name Pattern |
|---|---|
| Auth.API | `warehouse-auth-${shortdate}.log` |
| Customers.API | `warehouse-customers-api-${shortdate}.log` |
| Inventory.API | `warehouse-inventory-api-${shortdate}.log` |
| Purchasing.API | `warehouse-purchasing-api-${shortdate}.log` |
| Fulfillment.API | `warehouse-fulfillment-api-${shortdate}.log` |
| Gateway | `gateway-${shortdate}.log` |

### 2.3 NLog Loki Target Configuration

Each service MUST configure a Loki target with the following settings:

| Setting | Value |
|---|---|
| `batchSize` | `200` entries |
| `taskDelayMilliseconds` | `500` ms |
| `endpoint` | `${environment:LOKI_ENDPOINT:default=http\://localhost\:3100}` |
| `orderWrites` | `true` |

The Loki target MUST push three labels per log entry:

| Label | Value | Purpose |
|---|---|---|
| `service` | Static service name (e.g., `warehouse-auth-api`) | Filter logs by service in Grafana |
| `level` | `${level:lowercase=true}` | Filter logs by severity |
| `correlation_id` | `${scopeproperty:CorrelationId}` | Correlate log entries across a single request |

The Loki service label MUST use the following canonical names:

| Service | Loki Label Value |
|---|---|
| Auth.API | `warehouse-auth-api` |
| Customers.API | `warehouse-customers-api` |
| Inventory.API | `warehouse-inventory-api` |
| Purchasing.API | `warehouse-purchasing-api` |
| Fulfillment.API | `warehouse-fulfillment-api` |
| Gateway | `warehouse-gateway` |

**Edge case:** If the Loki endpoint is unreachable, NLog MUST silently drop entries destined for the Loki target. The console and file targets MUST continue operating normally. Loki unavailability MUST NOT cause request failures or service degradation.

### 2.4 NLog Log Level Rules

All API services (Auth, Customers, Inventory, Purchasing, Fulfillment) MUST configure the following log level filtering rules:

| Rule | Logger Name | Level Filter | Behavior |
|---|---|---|---|
| 1 | `Microsoft.EntityFrameworkCore.*` | `maxlevel="Info"`, `final="true"` | Suppresses EF Core query noise (Debug/Trace) |
| 2 | `Microsoft.AspNetCore.*` | `maxlevel="Info"`, `final="true"` | Suppresses ASP.NET Core middleware noise |
| 3 | `*` (catch-all) | `minlevel="Info"` | Routes all remaining Info+ logs to all three targets |

The Gateway MUST configure the following rules (no EF Core rule since it has no database):

| Rule | Logger Name | Level Filter | Behavior |
|---|---|---|---|
| 1 | `Microsoft.AspNetCore.*` | `maxlevel="Info"`, `final="true"` | Suppresses ASP.NET Core noise |
| 2 | `Yarp.*` | `minlevel="Info"` | Captures YARP reverse proxy logs |
| 3 | `*` (catch-all) | `minlevel="Info"` | Routes all remaining Info+ logs to all three targets |

**Edge case:** The `final="true"` attribute on suppression rules MUST prevent suppressed loggers from matching subsequent rules. Without `final="true"`, suppressed logs would still be written by the catch-all rule.

### 2.5 NLog Bootstrap Configuration

Each service MUST configure NLog with the following XML root attributes:

| Attribute | Value | Purpose |
|---|---|---|
| `autoReload` | `true` | Hot-reload nlog.config changes without restart |
| `throwConfigExceptions` | `true` | Fail fast on invalid configuration |
| `internalLogLevel` | `Info` | NLog's own diagnostic level |
| `internalLogFile` | `${basedir}/logs/nlog-internal.log` | NLog's internal error log |

Each service MUST declare the Loki extension:

```xml
<extensions>
  <add assembly="NLog.Loki" />
</extensions>
```

### 2.6 Distributed Tracing (OpenTelemetry)

The system MUST register distributed tracing via the `AddWarehouseTracing(configuration, serviceName)` extension method in `ServiceCollectionExtensions.cs`.

The method MUST configure the following:

1. **Service resource:** `service.name` set to the provided `serviceName` parameter, `service.version` set to `"1.0.0"`.
2. **ASP.NET Core instrumentation:** Auto-trace all inbound HTTP requests EXCEPT paths starting with `/health` (filtered to reduce tracing noise).
3. **HttpClient instrumentation:** Auto-trace all outbound HTTP calls (inter-service communication).
4. **SQL Client instrumentation:** Auto-trace all database queries with `SetDbStatementForText = true` (captures SQL command text in trace spans).
5. **OTLP exporter:** Export traces via gRPC to the Jaeger collector at the configured endpoint.

The OTLP endpoint MUST be read from `configuration["OpenTelemetry:OtlpEndpoint"]` with a default of `"http://localhost:4317"`.

**Trace propagation:** The system MUST use W3C TraceContext headers (OpenTelemetry default propagation format). The correlation ID from `CorrelationIdMiddleware` coexists with trace context — both are available in logs but serve different purposes (correlation ID for log grouping, trace ID for distributed trace visualization).

**Edge case:** If the Jaeger endpoint is unreachable, OpenTelemetry MUST silently drop traces. Jaeger unavailability MUST NOT cause request failures, exceptions, or service degradation.

### 2.7 Service Tracing Registration

Each service MUST call `AddWarehouseTracing` with its canonical service name:

| Service | Service Name | Registration Location |
|---|---|---|
| Auth.API | `warehouse-auth-api` | `Program.cs` |
| Customers.API | `warehouse-customers-api` | `Program.cs` |
| Inventory.API | `warehouse-inventory-api` | `Program.cs` |
| Purchasing.API | `warehouse-purchasing-api` | `Program.cs` |
| Fulfillment.API | `warehouse-fulfillment-api` | `Program.cs` |
| Gateway | `warehouse-gateway` | `Program.cs` |

The service name used for tracing MUST match the service name used in the Loki `service` label. This ensures logs and traces can be correlated by service identity.

### 2.8 Grafana Log Visualization

The system MUST provision Grafana with a pre-configured Loki data source.

| Setting | Value |
|---|---|
| URL | `http://localhost:3001` (Docker host port) |
| Default credentials | `admin` / `admin` |
| Loki data source | Pre-provisioned as default, non-editable |

Users SHOULD be able to query logs in Grafana by:
- Service name (Loki `service` label)
- Log level (Loki `level` label)
- Correlation ID (Loki `correlation_id` label or full-text search)
- Time range

### 2.9 Jaeger Trace Visualization

The system MUST provide a Jaeger UI for viewing distributed traces.

| Setting | Value |
|---|---|
| UI URL | `http://localhost:16686` |
| OTLP gRPC port | `4317` |
| OTLP HTTP port | `4318` |

Each trace SHOULD show the full span tree: inbound HTTP request, database queries (with SQL text), and any outbound HTTP calls.

### 2.10 Non-Blocking Guarantee

Observability infrastructure MUST be non-blocking. Specifically:

- Logging failures (Loki unreachable, file write error) MUST NOT cause HTTP 500 responses or request processing failures.
- Tracing failures (Jaeger unreachable, OTLP export error) MUST NOT cause HTTP 500 responses or request processing failures.
- Startup MUST NOT fail if Loki or Jaeger containers are not running. Only NLog config parse errors (when `throwConfigExceptions="true"`) MAY cause startup failure.
- Console and file targets MUST continue working independently of Loki availability.

---

## 3. Validation Rules

### 3.1 NLog Configuration

| # | Setting | Rule | Consequence |
|---|---|---|---|
| V1 | `nlog.config` | MUST exist in each service's project directory and be copied to the output directory | Service starts without structured logging |
| V2 | NLog extension | `NLog.Loki` assembly MUST be declared in `<extensions>` | Loki target type is unrecognized; NLog ignores it |
| V3 | Console target | MUST be named `console` and use the standard layout | Inconsistent log format across output channels |
| V4 | File target | MUST be named `file` with `archiveEvery="Day"` and `maxArchiveFiles="30"` | Log files accumulate indefinitely |
| V5 | Loki target | MUST include `service`, `level`, and `correlation_id` labels | Logs cannot be filtered by service or correlated |
| V6 | Loki `service` label | MUST match the canonical service name from Section 2.3 | Cross-service log queries return incomplete results |

### 3.2 OpenTelemetry Configuration

| # | Setting | Rule | Consequence |
|---|---|---|---|
| V7 | `AddWarehouseTracing` | MUST be called in each service's `Program.cs` | Service produces no traces |
| V8 | Service name | MUST match the canonical name from Section 2.7 | Traces appear under incorrect service in Jaeger |
| V9 | Health endpoint filter | ASP.NET Core instrumentation MUST filter paths starting with `/health` | Health check noise pollutes trace data |
| V10 | SQL Client instrumentation | `SetDbStatementForText` MUST be `true` | Database query text missing from trace spans |

### 3.3 Configuration Keys

| # | Key | Rule | Default |
|---|---|---|---|
| V11 | `LOKI_ENDPOINT` (env var) | SHOULD be set in Docker environments pointing to the Loki container | `http://localhost:3100` |
| V12 | `OpenTelemetry:OtlpEndpoint` | SHOULD be set in Docker environments pointing to the Jaeger container | `http://localhost:4317` |

---

## 4. Error Rules

| # | Condition | Impact | Behavior |
|---|---|---|---|
| E1 | Loki container unreachable | No centralized log aggregation | NLog silently drops Loki target entries; console and file targets continue. No HTTP errors. |
| E2 | Jaeger container unreachable | No distributed tracing | OpenTelemetry silently drops trace exports. No HTTP errors. |
| E3 | Invalid `nlog.config` XML | Service fails to start | NLog throws on startup because `throwConfigExceptions="true"`. This is intentional — misconfigured logging MUST be caught immediately. |
| E4 | Missing `nlog.config` file | No structured logging | NLog falls back to internal defaults. Logs MAY not appear in expected targets. |
| E5 | File system write failure (log directory) | File target stops writing | NLog silently skips file target. Console and Loki targets continue. |
| E6 | Loki returns HTTP error (429, 500, etc.) | Temporary log loss to Loki | NLog Loki target silently drops the batch. Retries on next batch cycle. |
| E7 | Invalid `OpenTelemetry:OtlpEndpoint` URI | No trace export | OpenTelemetry fails to create exporter. Traces are silently dropped. No service impact. |

**Design principle:** Observability MUST NOT introduce failure modes into request processing. All observability errors are silent degradations, not service errors.

---

## 5. Docker Infrastructure

### 5.1 Container Definitions

| Container | Image | Ports | Purpose |
|---|---|---|---|
| `warehouse-loki` | `grafana/loki:3.4.2` | `3100:3100` | Log aggregation engine |
| `warehouse-grafana` | `grafana/grafana:11.5.2` | `3001:3000` | Log visualization and querying |
| `warehouse-jaeger` | `jaegertracing/all-in-one:latest` | `4317:4317`, `4318:4318`, `16686:16686` | Trace collection, storage, and UI |

All containers are managed via `docker-compose.infrastructure.yml` and MUST have `restart: unless-stopped`.

### 5.2 Loki Configuration

Loki MUST be configured with the following settings (via `docker/loki/loki-config.yml`):

| Setting | Value | Rationale |
|---|---|---|
| `auth_enabled` | `false` | Single-tenant development setup |
| `http_listen_port` | `3100` | Standard Loki port |
| Storage backend | `filesystem` | Local development; no object store needed |
| Schema | `v13` with `tsdb` store | Current recommended Loki schema |
| Retention period | `30d` | Match file log retention |
| Compaction interval | `10m` | Regular compaction for disk efficiency |

### 5.3 Grafana Provisioning

Grafana MUST be provisioned with:

- A Loki data source pointing to `http://loki:3100` (Docker network)
- The data source MUST be marked as default and non-editable
- Anonymous access MUST be enabled with `Viewer` role for development convenience

### 5.4 Jaeger Configuration

Jaeger MUST run with `COLLECTOR_OTLP_ENABLED=true` to accept OpenTelemetry traces via OTLP gRPC (port 4317) and OTLP HTTP (port 4318).

---

## 6. Versioning Notes

- **v1 -- Initial specification (2026-04-07)**
  - Documents already-implemented centralized logging (NLog, Loki, Grafana)
  - Documents already-implemented distributed tracing (OpenTelemetry, Jaeger)
  - Covers all six services: Auth, Customers, Inventory, Purchasing, Fulfillment, Gateway
  - Non-blocking guarantee for all observability components

---

## 7. Test Plan

### Unit Tests -- TracingConfigurationTests

- `AddWarehouseTracing_RegistersOpenTelemetry` [Unit] -- Verifies that calling `AddWarehouseTracing` registers the OpenTelemetry `TracerProvider` in the service collection.
- `AddWarehouseTracing_ConfiguresAspNetCoreInstrumentation` [Unit] -- Verifies that ASP.NET Core auto-instrumentation is registered.
- `AddWarehouseTracing_ConfiguresHttpClientInstrumentation` [Unit] -- Verifies that HttpClient auto-instrumentation is registered.
- `AddWarehouseTracing_ConfiguresSqlClientInstrumentation` [Unit] -- Verifies that SQL Client auto-instrumentation is registered with `SetDbStatementForText = true`.
- `AddWarehouseTracing_ExcludesHealthEndpoints` [Unit] -- Verifies that the ASP.NET Core instrumentation filter excludes paths starting with `/health`.
- `AddWarehouseTracing_SetsServiceNameAndVersion` [Unit] -- Verifies that the service resource is configured with the provided service name and version `"1.0.0"`.
- `AddWarehouseTracing_UsesCustomOtlpEndpoint` [Unit] -- Verifies that a non-default `OpenTelemetry:OtlpEndpoint` value is used as the OTLP exporter endpoint.

### Unit Tests -- NLogConfigurationTests

- `NLogConfig_HasConsoleTarget` [Unit] -- Verifies that the `nlog.config` file defines a `console` target of type `Console`.
- `NLogConfig_HasFileTarget` [Unit] -- Verifies that the `nlog.config` file defines a `file` target of type `File` with daily rotation and 30-day retention.
- `NLogConfig_HasLokiTarget` [Unit] -- Verifies that the `nlog.config` file defines a `loki` target of type `loki` with batch size 200 and task delay 500ms.
- `NLogConfig_SuppressesEfCoreVerboseLogging` [Unit] -- Verifies that the `Microsoft.EntityFrameworkCore.*` logger has `maxlevel="Info"` and `final="true"`.
- `NLogConfig_SuppressesAspNetCoreVerboseLogging` [Unit] -- Verifies that the `Microsoft.AspNetCore.*` logger has `maxlevel="Info"` and `final="true"`.
- `NLogConfig_IncludesCorrelationIdInLayout` [Unit] -- Verifies that all target layouts include `${scopeproperty:CorrelationId}`.

### Integration Tests -- ObservabilityIntegrationTests

- `Request_WithCorrelationId_AppearsInLogOutput` [Integration] -- Sends an HTTP request with an `X-Correlation-ID` header and verifies the correlation ID appears in captured log output.
- `Request_GeneratesTrace_WithCorrectServiceName` [Integration] -- Sends an HTTP request and verifies that a trace activity is created with the expected service name.
- `HealthEndpoint_ExcludedFromTracing` [Integration] -- Sends a request to `/health/live` and verifies that no trace activity is created for it.

---

## Key Files

| File | Purpose |
|---|---|
| `src/Warehouse.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | `AddWarehouseTracing` extension method |
| `src/Warehouse.Infrastructure/Middleware/CorrelationIdMiddleware.cs` | Sets `CorrelationId` in NLog scope context |
| `src/Warehouse.Infrastructure/Http/CorrelationIdDelegatingHandler.cs` | Propagates correlation ID to outbound HTTP calls |
| `src/Interfaces/Auth/Warehouse.Auth.API/nlog.config` | Auth service NLog configuration |
| `src/Interfaces/Customers/Warehouse.Customers.API/nlog.config` | Customers service NLog configuration |
| `src/Interfaces/Inventory/Warehouse.Inventory.API/nlog.config` | Inventory service NLog configuration |
| `src/Interfaces/Purchasing/Warehouse.Purchasing.API/nlog.config` | Purchasing service NLog configuration |
| `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/nlog.config` | Fulfillment service NLog configuration |
| `src/Gateway/Warehouse.Gateway/nlog.config` | Gateway NLog configuration (no EF Core rule, adds Yarp rule) |
| `docker-compose.infrastructure.yml` | Loki, Grafana, Jaeger container definitions |
| `docker/loki/loki-config.yml` | Loki server configuration |
| `docker/grafana/provisioning/datasources/loki.yml` | Grafana Loki data source provisioning |
