namespace Warehouse.ServiceModel.Requests.Auth;

/// <summary>
/// Request payload for assigning roles to a user.
/// </summary>
public sealed record AssignRolesRequest
{
    /// <summary>
    /// Gets the collection of role IDs to assign.
    /// </summary>
    public required IReadOnlyList<int> RoleIds { get; init; }
}
