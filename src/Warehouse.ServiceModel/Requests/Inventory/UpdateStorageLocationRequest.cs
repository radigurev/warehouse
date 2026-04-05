namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for updating an existing storage location.
/// </summary>
public sealed record UpdateStorageLocationRequest
{
    /// <summary>
    /// Gets the location name. Required, 1-100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the location type (Row, Shelf, Bin, Bulk). Required.
    /// </summary>
    public required string LocationType { get; init; }

    /// <summary>
    /// Gets the optional capacity.
    /// </summary>
    public decimal? Capacity { get; init; }
}
