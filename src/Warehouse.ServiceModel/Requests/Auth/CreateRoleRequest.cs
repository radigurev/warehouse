namespace Warehouse.ServiceModel.Requests.Auth;

/// <summary>
/// Request payload for creating a new role.
/// </summary>
public sealed record CreateRoleRequest
{
    /// <summary>
    /// Gets the role name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional role description.
    /// </summary>
    public string? Description { get; init; }
}
