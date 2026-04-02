namespace Warehouse.ServiceModel.Requests.Auth;

/// <summary>
/// Request payload for creating a new permission.
/// </summary>
public sealed record CreatePermissionRequest
{
    /// <summary>
    /// Gets the resource identifier.
    /// </summary>
    public required string Resource { get; init; }

    /// <summary>
    /// Gets the action type.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; init; }
}
