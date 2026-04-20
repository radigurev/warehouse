# SDD-AUTH-001 — Authentication and Authorization

> Status: Implemented
> Last updated: 2026-04-19
> Owner: TBD
> Category: Infrastructure

## 1. Context & Scope

This spec defines the authentication and authorization system for all Warehouse microservices. It covers user identity management, JWT token lifecycle, role-based access control (RBAC), and user action auditing.

**In scope:**
- User registration, management, and deactivation
- JWT access token + refresh token authentication
- Role and permission management
- Role-based endpoint authorization
- User action audit logging
- Password hashing and policy enforcement

**Out of scope:**
- OAuth2 / external identity providers (future enhancement)
- Multi-factor authentication (future enhancement)
- API key authentication for service-to-service calls (future, if needed)

**Related specs:** None yet — this is the first spec. All future service specs will reference SDD-AUTH-001 for their authorization requirements.

**ISA-95 Conformance:** Conforms to ISA-95 Part 2, Section 6 — Personnel Model (Person, Personnel Class, Qualification, Credential).

---

## 2. Behavior

### 2.1 User Management

- The system MUST support creating users with username, email, password, first name, and last name.
- The system MUST enforce unique usernames and unique emails across all users.
- The system MUST hash passwords using BCrypt before storage. Plaintext passwords MUST NOT be stored.
- The system MUST support soft-delete (deactivation) of users. Deactivated users MUST NOT be able to authenticate.
- The system MUST support updating user profile fields (email, first name, last name).
- The system MUST support password changes. The current password MUST be verified before accepting a new password.
- The system SHOULD support listing users with pagination and filtering.
- The system MAY support searching users by username or email.

**Edge cases:**
- Creating a user with an existing username MUST return a 409 Conflict error.
- Creating a user with an existing email MUST return a 409 Conflict error.
- Updating a deactivated user MUST return a 404 Not Found error (deactivated users are invisible to management endpoints).

### 2.2 Authentication (JWT)

- The system MUST authenticate users via username + password and return a JWT access token and a refresh token.
- Access tokens MUST be signed using HMAC-SHA256 (HS256) with a server-side secret key.
- Access tokens MUST expire after 30 minutes.
- Access tokens MUST contain the following claims: `sub` (user ID), `username`, `email`, `roles` (array), `jti` (unique token ID), `iat`, `exp`.
- Refresh tokens MUST be opaque random strings stored in the database.
- Refresh tokens MUST expire after 7 days.
- The system MUST support exchanging a valid, non-expired, non-revoked refresh token for a new access token + refresh token pair (token rotation).
- On refresh, the old refresh token MUST be revoked immediately.
- The system MUST support explicit logout by revoking the refresh token.
- The system MUST reject authentication attempts for deactivated users.

**Edge cases:**
- Login with invalid credentials MUST return a 401 Unauthorized error. The error MUST NOT distinguish between "user not found" and "wrong password" (prevent enumeration).
- Refresh with an expired token MUST return a 401 Unauthorized error.
- Refresh with an already-revoked token MUST return a 401 Unauthorized error and SHOULD revoke all refresh tokens for that user (potential token theft detection).

### 2.3 Authorization (RBAC)

- The system MUST support defining roles with a name and description.
- The system MUST support defining permissions as resource + action pairs (e.g., `users:read`, `inventory:write`, `orders:delete`).
- The system MUST support assigning multiple roles to a user (M:N relationship).
- The system MUST support assigning multiple permissions to a role (M:N relationship).
- The system MUST enforce permission-based authorization on all protected endpoints. Permissions are checked by resolving the user's roles → role permissions from the database.
- The system MUST provide a `[RequirePermission("resource:action")]` attribute that controllers use to declare required permissions.
- The system MUST provide a reusable `PermissionAuthorizationHandler` middleware that other services can adopt.
- The system MUST include a built-in `Admin` role with full permissions. This role MUST NOT be deletable.

**Edge cases:**
- Deleting a role that is assigned to users MUST fail with a 409 Conflict error listing the affected user count.
- Assigning a non-existent role to a user MUST return a 404 Not Found error.

### 2.4 User Action Audit

- Authentication events (login, logout, token refresh) MUST be logged via structured NLog logging (routed to Loki/Grafana). They are NOT written to the audit database table.
- The system MUST log all user management events (create, update, deactivate, password change, role assignment) to the `auth.UserActionLogs` database table via `AuditService`.
- Each database audit entry MUST record: user ID, action, resource, details (JSON), IP address, and timestamp.
- Audit logs MUST be immutable — no update or delete operations.
- The system SHOULD support querying audit logs by user ID with pagination.
- The system MAY support filtering audit logs by action type and date range.

### 2.5 Database Seeding

- On startup, the system MUST create the `Admin` role (with `IsSystem = true`) if it does not exist.
- On startup, the system MUST create a default `admin` user (username: `admin`, password: `Admin123!`) with the Admin role if no admin user exists.
- The seed user is intended for initial setup only. The password SHOULD be changed after first login in production.

#### 2.5.1 Seeded Permission Catalog

The seeder MUST ensure the following permissions exist in the `auth.Permissions` table and are assigned to the `Admin` role. Other roles receive selective grants as noted.

| Permission | Purpose | Seeded to `Admin` | Additional role grants |
|---|---|---|---|
| `product-prices:read` | Read the Fulfillment product price catalog (see `SDD-FULF-001` §2.10, CHG-FEAT-007) | Yes | Any role holding `sales-orders:create` MUST also receive this permission so sales order creators can view effective prices. |
| `product-prices:create` | Create a new catalog entry | Yes | — |
| `product-prices:update` | Update `UnitPrice`, `ValidFrom`, `ValidTo` on a catalog entry | Yes | — |
| `product-prices:delete` | Delete a catalog entry (historical SO lines retain their snapshot `UnitPrice`) | Yes | — |

**Rule (cascading read grant):** When the seeder adds `sales-orders:create` to a role, it MUST also add `product-prices:read` to that role. This guarantees that SO line create/update flows (which depend on `IProductPriceResolver`) can always preview the resolved price through the diagnostic endpoint.

---

## 3. Validation Rules

### 3.1 User Creation / Update

| # | Field | Rule | Error Code |
|---|---|---|---|
| V1 | Username | Required. 3–50 characters. Alphanumeric + underscores only. | `INVALID_USERNAME` |
| V2 | Email | Required. Valid email format (RFC 5322). Max 256 characters. | `INVALID_EMAIL` |
| V3 | Password | Required on create. Min 8 characters. Must contain at least one uppercase, one lowercase, and one digit. | `INVALID_PASSWORD` |
| V4 | FirstName | Required. 1–100 characters. | `INVALID_FIRST_NAME` |
| V5 | LastName | Required. 1–100 characters. | `INVALID_LAST_NAME` |
| V6 | Username | Must be unique across active users. | `DUPLICATE_USERNAME` |
| V7 | Email | Must be unique across active users. | `DUPLICATE_EMAIL` |

### 3.2 Role Management

| # | Field | Rule | Error Code |
|---|---|---|---|
| V8 | Role Name | Required. 2–50 characters. Alphanumeric + spaces. Unique. | `INVALID_ROLE_NAME` |
| V9 | Role Description | Optional. Max 500 characters. | `INVALID_ROLE_DESCRIPTION` |
| V10 | Role Name | Must be unique across all roles. | `DUPLICATE_ROLE_NAME` |

### 3.3 Permission Definition

| # | Field | Rule | Error Code |
|---|---|---|---|
| V11 | Resource | Required. Lowercase alphanumeric + dots (e.g., `inventory.products`). Max 100 characters. | `INVALID_RESOURCE` |
| V12 | Action | Required. One of: `read`, `write`, `update`, `delete`, `all`. | `INVALID_ACTION` |
| V13 | Resource + Action | Combination must be unique. | `DUPLICATE_PERMISSION` |

### 3.4 Authentication

| # | Field | Rule | Error Code |
|---|---|---|---|
| V14 | Username | Required on login. | `MISSING_USERNAME` |
| V15 | Password | Required on login. | `MISSING_PASSWORD` |
| V16 | RefreshToken | Required on refresh. | `MISSING_REFRESH_TOKEN` |

---

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | Invalid credentials (wrong username or password) | 401 | `INVALID_CREDENTIALS` | Invalid username or password. |
| E2 | Deactivated user attempts login | 401 | `INVALID_CREDENTIALS` | Invalid username or password. |
| E3 | Expired access token | 401 | `TOKEN_EXPIRED` | Access token has expired. |
| E4 | Invalid or malformed access token | 401 | `INVALID_TOKEN` | Access token is invalid. |
| E5 | Expired refresh token | 401 | `REFRESH_TOKEN_EXPIRED` | Refresh token has expired. |
| E6 | Revoked refresh token (potential theft) | 401 | `REFRESH_TOKEN_REVOKED` | Refresh token has been revoked. |
| E7 | Insufficient permissions | 403 | `FORBIDDEN` | You do not have permission to perform this action. |
| E8 | User not found | 404 | `USER_NOT_FOUND` | User not found. |
| E9 | Role not found | 404 | `ROLE_NOT_FOUND` | Role not found. |
| E10 | Duplicate username | 409 | `DUPLICATE_USERNAME` | A user with this username already exists. |
| E11 | Duplicate email | 409 | `DUPLICATE_EMAIL` | A user with this email already exists. |
| E12 | Delete role with assigned users | 409 | `ROLE_IN_USE` | Cannot delete role — it is assigned to {count} user(s). |
| E13 | Validation failure | 400 | `VALIDATION_ERROR` | One or more fields are invalid. (ProblemDetails with field errors) |
| E14 | Wrong current password on password change | 400 | `INVALID_CURRENT_PASSWORD` | Current password is incorrect. |
| E15 | Duplicate role name | 409 | `DUPLICATE_ROLE_NAME` | A role with this name already exists. |
| E16 | Delete Admin role | 400 | `PROTECTED_ROLE` | The Admin role cannot be deleted. |
| E17 | Duplicate permission | 409 | `DUPLICATE_PERMISSION` | A permission with this resource and action already exists. |

All error responses MUST use ProblemDetails (RFC 7807) format:

```json
{
  "type": "https://warehouse.local/errors/{error-code}",
  "title": "Short error title",
  "status": 400,
  "detail": "Human-readable description.",
  "instance": "/api/v1/auth/login",
  "errors": {}
}
```

---

## 5. API Endpoints

| Method | Route | Description | Auth Required | Permission |
|---|---|---|---|---|
| POST | `/api/v1/auth/login` | Authenticate user | No | — |
| POST | `/api/v1/auth/refresh` | Refresh access token | No | — |
| POST | `/api/v1/auth/logout` | Revoke refresh token | Yes | — |
| GET | `/api/v1/users` | List users (paginated) | Yes | `users:read` |
| GET | `/api/v1/users/{id}` | Get user by ID | Yes | `users:read` |
| POST | `/api/v1/users` | Create user | Yes | `users:write` |
| PUT | `/api/v1/users/{id}` | Update user profile | Yes | `users:update` |
| DELETE | `/api/v1/users/{id}` | Deactivate user | Yes | `users:delete` |
| PUT | `/api/v1/users/{id}/password` | Change password | Yes | Own user or `users:update` |
| GET | `/api/v1/users/{id}/roles` | Get roles for user | Yes | `users:read` |
| POST | `/api/v1/users/{id}/roles` | Assign roles to user | Yes | `users:update` |
| DELETE | `/api/v1/users/{id}/roles/{roleId}` | Remove role from user | Yes | `users:update` |
| GET | `/api/v1/roles` | List all roles | Yes | `roles:read` |
| GET | `/api/v1/roles/{id}` | Get role by ID | Yes | `roles:read` |
| POST | `/api/v1/roles` | Create role | Yes | `roles:write` |
| PUT | `/api/v1/roles/{id}` | Update role | Yes | `roles:update` |
| DELETE | `/api/v1/roles/{id}` | Delete role | Yes | `roles:delete` |
| GET | `/api/v1/roles/{id}/permissions` | Get permissions for role | Yes | `roles:read` |
| POST | `/api/v1/roles/{id}/permissions` | Assign permissions to role | Yes | `roles:update` |
| DELETE | `/api/v1/roles/{id}/permissions/{permissionId}` | Remove permission from role | Yes | `roles:update` |
| GET | `/api/v1/permissions` | List all permissions | Yes | `roles:read` |
| POST | `/api/v1/permissions` | Create permission | Yes | `roles:write` |
| GET | `/api/v1/audit` | Query audit logs | Yes | `audit:read` |
| GET | `/api/v1/audit/users/{userId}` | Audit logs for user | Yes | `audit:read` |

---

## 6. Database Schema

**Schema name:** `auth`

### Tables

**auth.Users**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| Username | NVARCHAR(50) | NOT NULL, UNIQUE |
| Email | NVARCHAR(256) | NOT NULL, UNIQUE |
| PasswordHash | NVARCHAR(512) | NOT NULL |
| FirstName | NVARCHAR(100) | NOT NULL |
| LastName | NVARCHAR(100) | NOT NULL |
| IsActive | BIT | NOT NULL, DEFAULT 1 |
| CreatedAt | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| UpdatedAt | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |

**auth.Roles**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| Name | NVARCHAR(50) | NOT NULL, UNIQUE |
| Description | NVARCHAR(500) | NULL |
| IsSystem | BIT | NOT NULL, DEFAULT 0 |
| CreatedAt | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |

**auth.Permissions**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| Resource | NVARCHAR(100) | NOT NULL |
| Action | NVARCHAR(20) | NOT NULL |
| Description | NVARCHAR(500) | NULL |
| CreatedAt | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| | | UNIQUE(Resource, Action) |

**auth.UserRoles**

| Column | Type | Constraints |
|---|---|---|
| UserId | INT | PK, FK → auth.Users(Id) |
| RoleId | INT | PK, FK → auth.Roles(Id) |
| AssignedAt | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |

**auth.RolePermissions**

| Column | Type | Constraints |
|---|---|---|
| RoleId | INT | PK, FK → auth.Roles(Id) |
| PermissionId | INT | PK, FK → auth.Permissions(Id) |
| AssignedAt | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |

**auth.RefreshTokens**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| UserId | INT | NOT NULL, FK → auth.Users(Id) |
| Token | NVARCHAR(512) | NOT NULL, UNIQUE |
| ExpiresAt | DATETIME2(7) | NOT NULL |
| CreatedAt | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| RevokedAt | DATETIME2(7) | NULL |

**auth.UserActionLogs**

| Column | Type | Constraints |
|---|---|---|
| Id | BIGINT | PK, IDENTITY(1,1) |
| UserId | INT | NULL, FK → auth.Users(Id) |
| Action | NVARCHAR(50) | NOT NULL |
| Resource | NVARCHAR(100) | NOT NULL |
| Details | NVARCHAR(MAX) | NULL (JSON) |
| IpAddress | NVARCHAR(45) | NULL |
| CreatedAt | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |

### Indexes

| Name | Table | Columns | Type |
|---|---|---|---|
| IX_Users_Username | auth.Users | Username | Unique |
| IX_Users_Email | auth.Users | Email | Unique |
| IX_Users_IsActive | auth.Users | IsActive | Non-unique (filtered: IsActive = 1) |
| IX_RefreshTokens_Token | auth.RefreshTokens | Token | Unique |
| IX_RefreshTokens_UserId | auth.RefreshTokens | UserId | Non-unique |
| IX_UserActionLogs_UserId | auth.UserActionLogs | UserId, CreatedAt DESC | Non-unique |
| IX_UserActionLogs_Action | auth.UserActionLogs | Action, CreatedAt DESC | Non-unique |
| IX_Permissions_Resource_Action | auth.Permissions | Resource, Action | Unique |

---

## 7. Versioning Notes

- **v1 — Initial specification (2026-04-03)**
  - Core authentication with JWT access + refresh tokens
  - RBAC with users, roles, permissions
  - User action audit logging
  - ProblemDetails error responses
- **v2 — Post-validation alignment (2026-04-03)** (non-breaking)
  - Auth events moved from AuditService to NLog (Loki/Grafana)
  - Added database seeding behavior (admin user + role)
  - Permission-based authorization via `[RequirePermission]` attribute
  - Updated test plan to match implemented tests
  - Updated Key Files to reflect Data Annotations approach
  - Status changed from Draft to Implemented

- **v2.1 — CHG-FEAT-007 seeded permission catalog (2026-04-19)** (non-breaking additive)
  - Seeder gains four new permissions: `product-prices:read|create|update|delete` (see `SDD-FULF-001` §2.10).
  - All four are assigned to the `Admin` role.
  - `product-prices:read` additionally cascades to any role that holds `sales-orders:create`, so sales order creators can preview catalog prices.
  - No schema changes; additions are data-only via `DatabaseSeeder`.

---

## 8. Test Plan

### Unit Tests — PasswordHasherTests

- `Hash_ReturnsNonEmptyString` [Unit]
- `Hash_ReturnsBCryptFormat` [Unit]
- `Hash_SamePasswordProducesDifferentHashes` [Unit]
- `Hash_DifferentPasswordsProduceDifferentHashes` [Unit]
- `Verify_CorrectPassword_ReturnsTrue` [Unit]
- `Verify_WrongPassword_ReturnsFalse` [Unit]
- `Verify_EmptyPassword_ReturnsFalse` [Unit]
- `Verify_CaseSensitive` [Unit]

### Unit Tests — JwtTokenServiceTests

- `GenerateAccessToken_ReturnsNonEmptyToken` [Unit]
- `GenerateAccessToken_ReturnsValidJwt` [Unit]
- `GenerateAccessToken_ContainsSubClaim` [Unit]
- `GenerateAccessToken_ContainsUsernameClaim` [Unit]
- `GenerateAccessToken_ContainsEmailClaim` [Unit]
- `GenerateAccessToken_ContainsRoleClaims` [Unit]
- `GenerateAccessToken_ContainsJtiClaim` [Unit]
- `GenerateAccessToken_JtiIsUniquePerCall` [Unit]
- `GenerateAccessToken_ExpiresAtIsInFuture` [Unit]
- `GenerateAccessToken_HasCorrectIssuer` [Unit]
- `GenerateAccessToken_HasCorrectAudience` [Unit]
- `GenerateAccessToken_SignatureIsValid` [Unit]
- `GenerateAccessToken_UserWithNoRoles_HasNoRoleClaims` [Unit]
- `GenerateRefreshToken_ReturnsNonEmptyString` [Unit]
- `GenerateRefreshToken_IsBase64` [Unit]
- `GenerateRefreshToken_IsUniquePerCall` [Unit]

### Integration Tests — AuthControllerTests

- `Login_ValidCredentials_Returns200WithTokens` [Integration]
- `Login_WrongPassword_Returns401` [Integration]
- `Login_NonExistentUser_Returns401` [Integration]
- `Login_EmptyBody_Returns400` [Integration]
- `Login_DeactivatedUser_Returns401` [Integration]
- `Refresh_ValidToken_Returns200WithNewTokens` [Integration]
- `Refresh_OldTokenAfterRotation_RejectsRequest` [Integration]
- `Refresh_InvalidToken_Returns401` [Integration]
- `Logout_ValidToken_Returns204` [Integration]
- `Logout_RefreshTokenRevokedAfterLogout` [Integration]

### Integration Tests — UsersControllerTests

- `GetUsers_Authenticated_Returns200` [Integration]
- `GetUsers_Unauthenticated_Returns401` [Integration]
- `CreateUser_ValidPayload_Returns201` [Integration]
- `CreateUser_DuplicateUsername_Returns409` [Integration]
- `CreateUser_DuplicateEmail_Returns409` [Integration]
- `CreateUser_InvalidPassword_Returns400` [Integration]
- `GetUserById_ExistingUser_Returns200` [Integration]
- `UpdateUser_ValidPayload_Returns200` [Integration]
- `DeactivateUser_Returns204` [Integration]
- `DeactivatedUser_CannotLogin` [Integration]

### Integration Tests — RolesControllerTests

- `GetRoles_Returns200` [Integration]
- `CreateRole_ValidPayload_Returns201` [Integration]
- `CreateRole_DuplicateName_Returns409` [Integration]
- `DeleteRole_AdminRole_Returns400` [Integration]
- `DeleteRole_RoleWithUsers_Returns409` [Integration]

### Integration Tests — PermissionsControllerTests

- `GetPermissions_Returns200` [Integration]
- `CreatePermission_Returns201` [Integration]
- `CreatePermission_Duplicate_Returns409` [Integration]

---

## 9. Acceptance Criteria

- [ ] Users can register, login, and receive JWT tokens
- [ ] Access tokens expire after 30 minutes and can be refreshed
- [ ] Refresh token rotation is enforced (old token revoked on refresh)
- [ ] Revoked refresh token triggers revocation of all user tokens (theft detection)
- [ ] Passwords are hashed with BCrypt — never stored in plaintext
- [ ] RBAC enforced: users without required permissions receive 403
- [ ] Admin role exists by default and cannot be deleted
- [ ] Auth events logged via NLog (Loki); user management events logged to audit table
- [ ] All error responses use ProblemDetails format
- [ ] All validation rules enforced with appropriate error codes
- [ ] Deactivated users cannot authenticate or be managed

---

## Key Files

- `src/Interfaces/Warehouse.Auth.API/Controllers/AuthController.cs`
- `src/Interfaces/Warehouse.Auth.API/Controllers/UsersController.cs`
- `src/Interfaces/Warehouse.Auth.API/Controllers/RolesController.cs`
- `src/Interfaces/Warehouse.Auth.API/Controllers/PermissionsController.cs`
- `src/Interfaces/Warehouse.Auth.API/Controllers/AuditController.cs`
- `src/Databases/Warehouse.DBModel/Models/Auth/User.cs`
- `src/Databases/Warehouse.DBModel/Models/Auth/Role.cs`
- `src/Databases/Warehouse.DBModel/Models/Auth/Permission.cs`
- `src/Databases/Warehouse.DBModel/Models/Auth/UserRole.cs`
- `src/Databases/Warehouse.DBModel/Models/Auth/RolePermission.cs`
- `src/Databases/Warehouse.DBModel/Models/Auth/RefreshToken.cs`
- `src/Databases/Warehouse.DBModel/Models/Auth/UserActionLog.cs`
- `src/Databases/Warehouse.DBModel/WarehouseDbContext.cs`
- `src/Interfaces/Warehouse.Auth.API/Authorization/PermissionAuthorizationHandler.cs`
- `src/Interfaces/Warehouse.Auth.API.Tests/`
