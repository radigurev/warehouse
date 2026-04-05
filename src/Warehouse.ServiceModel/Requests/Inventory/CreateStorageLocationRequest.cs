namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for creating a new storage location.
/// </summary>
public sealed record CreateStorageLocationRequest
{
    /// <summary>
    /// Gets the zone ID. Required.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Gets the location code. Required, 1-30 characters.
    /// </summary>
    public required string Code { get; init; }

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
