namespace Warehouse.Common.ErrorCodes;

/// <summary>
/// Error codes for the Auth domain (users, roles, permissions, tokens).
/// </summary>
public static class AuthErrorCodes
{
    /// <summary>Login credentials are invalid.</summary>
    public const string InvalidCredentials = "INVALID_CREDENTIALS";

    /// <summary>Current password does not match.</summary>
    public const string InvalidCurrentPassword = "INVALID_CURRENT_PASSWORD";

    /// <summary>Refresh token has expired.</summary>
    public const string RefreshTokenExpired = "REFRESH_TOKEN_EXPIRED";

    /// <summary>Refresh token has been revoked.</summary>
    public const string RefreshTokenRevoked = "REFRESH_TOKEN_REVOKED";

    /// <summary>User not found.</summary>
    public const string UserNotFound = "USER_NOT_FOUND";

    /// <summary>Role not found.</summary>
    public const string RoleNotFound = "ROLE_NOT_FOUND";

    /// <summary>Permission not found.</summary>
    public const string PermissionNotFound = "PERMISSION_NOT_FOUND";

    /// <summary>Duplicate username.</summary>
    public const string DuplicateUsername = "DUPLICATE_USERNAME";

    /// <summary>Duplicate email address.</summary>
    public const string DuplicateEmail = "DUPLICATE_EMAIL";

    /// <summary>Duplicate role name.</summary>
    public const string DuplicateRoleName = "DUPLICATE_ROLE_NAME";

    /// <summary>Duplicate permission resource:action.</summary>
    public const string DuplicatePermission = "DUPLICATE_PERMISSION";

    /// <summary>Role is assigned to users and cannot be deleted.</summary>
    public const string RoleInUse = "ROLE_IN_USE";

    /// <summary>System role cannot be modified or deleted.</summary>
    public const string ProtectedRole = "PROTECTED_ROLE";
}
