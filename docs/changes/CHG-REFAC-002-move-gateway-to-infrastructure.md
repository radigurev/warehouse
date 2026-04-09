# CHG-REFAC-002 — Move YARP Gateway to Infrastructure Folder

> Status: Implemented
> Last updated: 2026-04-09
> Owner: TBD
> Priority: P3

## 1. Context & Scope

**Why this change is needed:**
The YARP API Gateway is a cross-cutting infrastructure concern (routing, rate limiting, health aggregation), not a business domain service. It was placed in `src/Gateway/` during initial scaffolding. With the `src/Infrastructure/` folder now established (EventLog.API moved there in a prior refactor), the Gateway should follow the same convention. This keeps `src/Interfaces/` exclusively for business domain APIs and `src/Infrastructure/` for infrastructure services.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [ ] Frontend changes
- [x] Configuration changes

## 2. Behavior (RFC 2119)

### Folder Structure

- `Warehouse.Gateway` MUST be moved from `src/Gateway/Warehouse.Gateway/` to `src/Infrastructure/Gateway/Warehouse.Gateway/`.
- The `src/Gateway/` folder MUST be deleted after the move.
- The solution file (`Warehouse.slnx`) MUST be updated to reflect the new project path.

### Runtime Behavior

- The Gateway MUST continue to function identically after the move — no changes to routing, rate limiting, health checks, or configuration.
- The Gateway port (5000) MUST remain unchanged.
- Docker Compose build context and Dockerfile paths MUST be updated to point to the new location.

### Project References

- `Warehouse.Gateway` has no project references (standalone) — no reference updates required.
- No other project references `Warehouse.Gateway` — no downstream changes.

### Documentation

- `CLAUDE.md` section 2 (Solution Structure) MUST be updated to show the new path.
- `CLAUDE.md` section 13 (Quick Reference — Key File Paths) MUST be updated.

## 3. Validation Rules

No validation rules — this is a structural refactor with no behavior change.

## 4. Error Rules

No error rules — this is a structural refactor with no behavior change.

## 5. Versioning Notes

**API version impact:** None

**Database migration required:** No

**Backwards compatibility:** Fully compatible — no API or behavior changes.

## 6. Test Plan

### Unit Tests

No new unit tests — no behavior change.

### Integration Tests

- [ ] `[Integration] Gateway_HealthEndpoint_ReturnsHealthy` — verify Gateway starts and responds on `/health` after the move
- [ ] `[Integration] Gateway_RoutesToBackendServices` — verify YARP routing still proxies to Auth, Customers, Inventory APIs

## 7. Detailed Design

### File System Changes

| Action | From | To |
|---|---|---|
| Move | `src/Gateway/Warehouse.Gateway/` | `src/Infrastructure/Gateway/Warehouse.Gateway/` |
| Delete | `src/Gateway/` (empty after move) | — |

### Solution File Changes

Update `Warehouse.slnx` project path:

```xml
<!-- Before -->
<Project Path="Gateway/Warehouse.Gateway/Warehouse.Gateway.csproj" />

<!-- After -->
<Project Path="Infrastructure/Gateway/Warehouse.Gateway/Warehouse.Gateway.csproj" />
```

### Docker Changes

Update `docker-compose.infrastructure.yml` build context for the Gateway service:

```yaml
# Before
warehouse-gateway:
  build:
    context: ./src
    dockerfile: Gateway/Warehouse.Gateway/Dockerfile

# After
warehouse-gateway:
  build:
    context: ./src
    dockerfile: Infrastructure/Gateway/Warehouse.Gateway/Dockerfile
```

Update the `Dockerfile` inside the Gateway project if it contains `COPY` paths relative to the old location.

### Documentation Changes

Update `CLAUDE.md`:
- Section 2 solution tree: `Gateway/` → `Infrastructure/Gateway/`
- Section 13 key file paths: `src/Gateway/Warehouse.Gateway/` → `src/Infrastructure/Gateway/Warehouse.Gateway/`

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-INFRA-001 | Gateway section path references updated |

## Migration Plan

1. **Pre-deployment:** Move folder, update solution file, update Docker paths, update CLAUDE.md. Build and verify locally.
2. **Deployment:** Standard deployment — no runtime changes.
3. **Post-deployment:** Verify Gateway health endpoint and routing in Docker Compose.
4. **Rollback:** Move folder back, revert solution and Docker changes.

## Open Questions

- [ ] Should the Gateway Dockerfile be regenerated or just patched for the new path?
