# CHG-ENH-004 — Real-Time Permission Validation via Redis

> Status: Implemented
> Last updated: 2026-04-18
> Owner: Radoslav
> Priority: P1

## 1. Context & Scope

**Why this change is needed:**
Permissions are currently embedded as claims in the JWT access token. This creates a security gap: if a permission or role assignment is revoked, the user retains the old permissions until the token expires (up to 30 minutes). Additionally, as the permission set grows the token size bloats, increasing bandwidth on every request. Moving to real-time Redis-backed permission lookups eliminates stale permissions and keeps tokens minimal.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [x] Frontend changes
- [x] Configuration changes

## 2. Behavior (RFC 2119)

### JWT Token Content

- The JWT access token MUST contain only identity claims: `sub` (userId), `username`, and `jti` (token ID).
- The JWT access token MUST NOT contain role or permission claims.
- The JWT refresh token behavior MUST remain unchanged.

### Permission Resolution

- All services MUST resolve user permissions at request time via `IPermissionService`, not from JWT claims.
- `IPermissionService` MUST first check Redis for cached permissions using key `auth:user:{userId}:permissions`.
- On cache miss, `IPermissionService` MUST call Auth.API to retrieve the user's resolved permission set.
- The resolved permission set MUST be cached in Redis with a TTL of 5 minutes.
- Permission resolution MUST NOT block the request if both Redis and Auth.API are unavailable — the request MUST be denied (fail-closed).

### Cache Invalidation

- When a role assignment changes (UserRole created/deleted), Auth.API MUST invalidate the Redis key `auth:user:{userId}:permissions` for the affected user.
- When a role's permissions change (RolePermission created/deleted), Auth.API MUST invalidate the Redis keys for ALL users assigned to that role.
- Auth.API SHOULD publish a `UserPermissionsChangedEvent` via MassTransit after invalidation, so consuming services can drop any local in-memory state if applicable.

### Authorization Attribute

- The `[RequirePermission]` attribute MUST be reworked to resolve permissions via `IPermissionService` instead of reading JWT claims.
- The attribute MUST return `403 Forbidden` with a ProblemDetails response when the user lacks the required permission.
- The attribute MUST return `401 Unauthorized` if the JWT is missing or invalid (unchanged behavior).

### Auth.API Internal Endpoint

- Auth.API MUST expose `GET /api/v1/users/{id}/permissions` returning the fully resolved permission set (all permissions from all assigned roles).
- This endpoint MUST require authentication but MUST NOT require a specific permission (it is an infrastructure endpoint used by the shared library).
- The response MUST return a flat array of permission strings (e.g., `["inventory:read", "inventory:write"]`).

### Frontend

- The frontend MUST request the current user's permissions from Auth.API after login and on token refresh.
- The frontend MUST use the permission set for UI element visibility (hiding/showing buttons, menu items).
- The frontend MUST NOT treat client-side permission checks as a security boundary — the backend is authoritative.

## 3. Validation Rules

| # | Field / Input | Rule | Error Code |
|---|---|---|---|
| V1 | `{id}` in `/users/{id}/permissions` | Must be a valid existing user ID | `USER_NOT_FOUND` |

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | JWT missing or invalid | 401 | `UNAUTHORIZED` | Authentication required |
| E2 | User lacks required permission | 403 | `FORBIDDEN` | You do not have permission to perform this action |
| E3 | User not found (permissions endpoint) | 404 | `USER_NOT_FOUND` | User not found |
| E4 | Redis and Auth.API both unavailable | 403 | `PERMISSION_CHECK_FAILED` | Unable to verify permissions — access denied |

## 5. Versioning Notes

**API version impact:** New endpoint added (`GET /api/v1/users/{id}/permissions`), existing endpoints unchanged.

**Database migration required:** No

**Backwards compatibility:** Breaking change for JWT consumers — tokens no longer contain permission/role claims. Frontend must be updated simultaneously.

## 6. Test Plan

### Unit Tests

- [x] `[Unit] PermissionService_CacheHit_ReturnsFromRedis` — verifies cached permissions are returned without calling Auth.API
- [x] `[Unit] PermissionService_CacheMiss_CallsAuthApi` — verifies fallback to HTTP call on cache miss
- [x] `[Unit] PermissionService_CacheMiss_PopulatesRedis` — verifies the result is written to Redis after HTTP call
- [x] `[Unit] PermissionService_BothUnavailable_ReturnsDenied` — verifies fail-closed behavior
- [x] `[Unit] RequirePermission_UserHasPermission_Allows` — verifies authorized request passes
- [x] `[Unit] RequirePermission_UserLacksPermission_Returns403` — verifies forbidden response
- [x] `[Unit] RequirePermission_NoToken_Returns401` — verifies unauthenticated request rejected
- [x] `[Unit] TokenService_GenerateAccessToken_NoPermissionClaims` — verifies JWT contains only identity claims
- [x] `[Unit] UserPermissionsCacheInvalidation_OnRoleAssignmentChange` — verifies Redis key deleted on UserRole change
- [x] `[Unit] UserPermissionsCacheInvalidation_OnRolePermissionChange` — verifies Redis keys deleted for all affected users

### Integration Tests

- [x] `[Integration] GetUserPermissions_ReturnsResolvedSet` — verifies endpoint returns correct flattened permissions
- [x] `[Integration] PermissionRevoked_NextRequestDenied` — verifies that revoking a permission and invalidating cache results in denial on next request
- [x] `[Integration] Login_TokenContainsOnlyIdentityClaims` — verifies login response JWT has no permission/role claims

## 7. Detailed Design

### API Changes

**New endpoint:**

```
GET /api/v1/users/{id}/permissions
Authorization: Bearer {token}

Response 200:
{
  "userId": 1,
  "permissions": ["inventory:read", "inventory:write", "customers:read"]
}
```

**Modified:** `POST /api/v1/auth/login` and `POST /api/v1/auth/refresh` — JWT payload reduced to identity claims only.

### Data Model Changes

None. The permission data model (User → UserRole → Role → RolePermission → Permission) is unchanged.

### Service Layer Changes

**New: `IPermissionService` / `PermissionService`** (in `Warehouse.Infrastructure`)
- `Task<IReadOnlySet<string>> GetPermissionsAsync(int userId, CancellationToken ct)`
- Redis lookup → HTTP fallback → cache populate
- Injected into all services via `services.AddWarehousePermissionValidation(configuration)`

**Modified: `TokenService`** (in `Warehouse.Auth.API`)
- Remove permission and role claims from `GenerateAccessToken()`
- Token payload: `sub`, `username`, `jti`, `iat`, `exp`

**Modified: `RequirePermissionAttribute` / `RequirePermissionHandler`** (in `Warehouse.Infrastructure`)
- Replace JWT claim check with `IPermissionService.GetPermissionsAsync(userId)`

**New: `UserPermissionsChangedEvent`** (in `Warehouse.ServiceModel/Events/`)
- Published when role assignments or role permissions change
- `sealed record UserPermissionsChangedEvent(int UserId, DateTime OccurredAt)`

**Modified: `UserService`** (in `Warehouse.Auth.API`)
- After UserRole changes: invalidate `auth:user:{userId}:permissions` in Redis
- After RolePermission changes: resolve affected user IDs, invalidate each key
- Publish `UserPermissionsChangedEvent`

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-AUTH-001 | JWT payload changes, permission validation reworked, new internal endpoint added |

## Migration Plan

1. **Pre-deployment:** Deploy Auth.API with the new `/users/{id}/permissions` endpoint while still embedding permissions in JWT (dual-mode). Verify endpoint works.
2. **Deployment:** Deploy all services with `IPermissionService` and updated `[RequirePermission]`. Deploy frontend with permission fetching. Remove permission claims from JWT generation.
3. **Post-deployment:** Monitor Redis hit rates and Auth.API call volume. Verify no 403 spikes.
4. **Rollback:** Re-enable permission claims in JWT, revert `[RequirePermission]` to claim-based. Frontend falls back gracefully (permissions still fetched from endpoint).

## Open Questions

- [ ] Should the permissions endpoint be restricted to same-user only (`/me/permissions`) or allow admin lookup of any user?
- [ ] Should consuming services cache permissions in-memory (L1) in addition to Redis (L2) for sub-millisecond checks?
- [ ] Should `UserPermissionsChangedEvent` trigger an immediate token revocation (blacklist the `jti`), or is cache invalidation sufficient?
