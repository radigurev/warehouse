namespace Warehouse.ServiceModel.DTOs.Auth;

/// <summary>
/// Represents a permission as a resource-action pair.
/// </summary>
public sealed record PermissionDto
{
    /// <summary>
    /// Gets the permission ID.
    /// </summary>
    public required int Id { get; init; }

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
