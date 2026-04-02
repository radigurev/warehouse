namespace Warehouse.ServiceModel.Requests.Auth;

/// <summary>
/// Request payload for assigning permissions to a role.
/// </summary>
public sealed record AssignPermissionsRequest
{
    /// <summary>
    /// Gets the collection of permission IDs to assign.
    /// </summary>
    public required IReadOnlyList<int> PermissionIds { get; init; }
}
