namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for creating a new warehouse.
/// </summary>
public sealed record CreateWarehouseRequest
{
    /// <summary>
    /// Gets the warehouse code. Required, 1-20 characters.
    /// </summary>
    public required string Code { get; init; }

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
