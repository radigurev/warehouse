namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for creating a new zone within a warehouse.
/// </summary>
public sealed record CreateZoneRequest
{
    /// <summary>
    /// Gets the warehouse ID. Required.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the zone code. Required, 1-20 characters.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the zone name. Required, 1-100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description. Max 500 characters.
    /// </summary>
    public string? Description { get; init; }
}
