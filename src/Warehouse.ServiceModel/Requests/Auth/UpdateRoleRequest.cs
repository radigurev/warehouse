namespace Warehouse.ServiceModel.Requests.Auth;

/// <summary>
/// Request payload for updating an existing role.
/// </summary>
public sealed record UpdateRoleRequest
{
    /// <summary>
    /// Gets the updated role name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the updated description.
    /// </summary>
    public string? Description { get; init; }
}
