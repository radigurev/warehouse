namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for updating an existing warehouse.
/// </summary>
public sealed record UpdateWarehouseRequest
{
    /// <summary>
    /// Gets the warehouse name. Required, 1-200 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional address. Max 500 characters.
    /// </summary>
    public string? Address { get; init; }

    /// <summary>
    /// Gets the optional notes. Max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }
}
