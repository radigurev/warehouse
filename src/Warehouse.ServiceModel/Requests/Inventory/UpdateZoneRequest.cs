namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for updating an existing zone.
/// </summary>
public sealed record UpdateZoneRequest
{
    /// <summary>
    /// Gets the zone name. Required, 1-100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description. Max 500 characters.
    /// </summary>
    public string? Description { get; init; }
}
