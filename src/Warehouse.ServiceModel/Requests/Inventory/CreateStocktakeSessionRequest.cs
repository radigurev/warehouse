namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for creating a new stocktake session.
/// </summary>
public sealed record CreateStocktakeSessionRequest
{
    /// <summary>
    /// Gets the warehouse ID. Required.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the optional zone ID to scope the session.
    /// </summary>
    public int? ZoneId { get; init; }

    /// <summary>
    /// Gets the session name. Required, 1-200 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional notes. Max 2000 characters.
    /// </summary>
    public string? Notes { get; init; }
}
