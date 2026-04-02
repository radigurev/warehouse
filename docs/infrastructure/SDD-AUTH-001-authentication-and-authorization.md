# SDD-AUTH-001 — Authentication and Authorization

> Status: Draft
> Last updated: 2026-04-03
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
- The system MUST enforce role-based authorization on all protected endpoints using the `roles` claim from the JWT.
- The system SHOULD provide a middleware component that other services can reuse for authorization.
- The system MUST include a built-in `Admin` role with full permissions. This role MUST NOT be deletable.

**Edge cases:**
- Deleting a role that is assigned to users MUST fail with a 409 Conflict error listing the affected user count.
- Assigning a non-existent role to a user MUST return a 404 Not Found error.

### 2.4 User Action Audit

- The system MUST log all authentication events (login success, login failure, token refresh, logout).
- The system MUST log all user management events (create, update, deactivate, password change, role assignment).
- Each audit entry MUST record: user ID, action, resource, details (JSON), IP address, and timestamp.
- Audit logs MUST be immutable — no update or delete operations.
- The system SHOULD support querying audit logs by user ID with pagination.
- The system MAY support filtering audit logs by action type and date range.

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

---

## 8. Test Plan

### Unit Tests

- `Login_ValidCredentials_ReturnsTokenPair` [Unit]
- `Login_InvalidPassword_ReturnsUnauthorized` [Unit]
- `Login_DeactivatedUser_ReturnsUnauthorized` [Unit]
- `Login_NonExistentUser_ReturnsUnauthorized` [Unit]
- `RefreshToken_ValidToken_ReturnsNewTokenPair` [Unit]
- `RefreshToken_ExpiredToken_ReturnsUnauthorized` [Unit]
- `RefreshToken_RevokedToken_ReturnsUnauthorizedAndRevokesAll` [Unit]
- `Logout_ValidToken_RevokesRefreshToken` [Unit]
- `CreateUser_ValidRequest_ReturnsCreatedUser` [Unit]
- `CreateUser_DuplicateUsername_ReturnsConflict` [Unit]
- `CreateUser_DuplicateEmail_ReturnsConflict` [Unit]
- `CreateUser_InvalidPassword_ReturnsValidationError` [Unit]
- `UpdateUser_ValidRequest_ReturnsUpdatedUser` [Unit]
- `UpdateUser_DeactivatedUser_ReturnsNotFound` [Unit]
- `DeactivateUser_ExistingUser_SetsIsActiveFalse` [Unit]
- `ChangePassword_ValidCurrentPassword_UpdatesHash` [Unit]
- `ChangePassword_WrongCurrentPassword_ReturnsBadRequest` [Unit]
- `CreateRole_ValidRequest_ReturnsCreatedRole` [Unit]
- `CreateRole_DuplicateName_ReturnsConflict` [Unit]
- `DeleteRole_RoleWithUsers_ReturnsConflict` [Unit]
- `DeleteRole_AdminRole_ReturnsBadRequest` [Unit]
- `AssignRoleToUser_ValidRole_AddsUserRole` [Unit]
- `AssignRoleToUser_NonExistentRole_ReturnsNotFound` [Unit]
- `CreatePermission_ValidRequest_ReturnsCreatedPermission` [Unit]
- `CreatePermission_DuplicateResourceAction_ReturnsConflict` [Unit]
- `AssignPermissionToRole_ValidPermission_AddsRolePermission` [Unit]

### Integration Tests

- `Login_ValidCredentials_Returns200WithTokens` [Integration]
- `Login_InvalidCredentials_Returns401ProblemDetails` [Integration]
- `RefreshToken_ValidToken_Returns200WithNewTokens` [Integration]
- `CreateUser_ValidPayload_Returns201` [Integration]
- `CreateUser_DuplicateUsername_Returns409ProblemDetails` [Integration]
- `CreateUser_InvalidPayload_Returns400ProblemDetails` [Integration]
- `GetUsers_Authenticated_Returns200WithPaginatedList` [Integration]
- `GetUsers_Unauthenticated_Returns401` [Integration]
- `GetUsers_InsufficientPermissions_Returns403` [Integration]
- `CreateRole_ValidPayload_Returns201` [Integration]
- `DeleteRole_InUse_Returns409ProblemDetails` [Integration]
- `AuditLog_AfterLogin_ContainsLoginEntry` [Integration]
- `PasswordChange_ValidRequest_Returns200` [Integration]
- `DeactivateUser_ThenLogin_Returns401` [Integration]

---

## 9. Acceptance Criteria

- [ ] Users can register, login, and receive JWT tokens
- [ ] Access tokens expire after 30 minutes and can be refreshed
- [ ] Refresh token rotation is enforced (old token revoked on refresh)
- [ ] Revoked refresh token triggers revocation of all user tokens (theft detection)
- [ ] Passwords are hashed with BCrypt — never stored in plaintext
- [ ] RBAC enforced: users without required permissions receive 403
- [ ] Admin role exists by default and cannot be deleted
- [ ] All auth and user management events are audit logged
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
- `src/Databases/Warehouse.DBModel/Configuration/Auth/UserConfiguration.cs`
- `src/Interfaces/Warehouse.Auth.API.Tests/`
