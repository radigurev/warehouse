namespace Warehouse.ServiceModel.Responses.Auth;

/// <summary>
/// Response containing the fully resolved permission set for a user.
/// </summary>
public sealed class UserPermissionsResponse
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the flat list of permission strings (e.g., "inventory:read").
    /// </summary>
    public IReadOnlyList<string> Permissions { get; set; } = [];
}
