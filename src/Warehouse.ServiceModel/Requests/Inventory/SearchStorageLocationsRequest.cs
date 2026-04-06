namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for searching storage locations with pagination.
/// </summary>
public sealed record SearchStorageLocationsRequest
{
    /// <summary>
    /// Gets the zone ID filter. Optional.
    /// </summary>
    public int? ZoneId { get; init; }

    /// <summary>
    /// Gets the warehouse ID filter. Optional.
    /// </summary>
    public int? WarehouseId { get; init; }

    /// <summary>
    /// Gets the name filter (contains match). Optional.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the code filter (starts-with match). Optional.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets the number of items per page. Defaults to 20.
    /// </summary>
    public int PageSize { get; init; } = 25;

    /// <summary>
    /// Gets the generic filter expression string. Optional.
    /// </summary>
    public string? Filter { get; init; }
}
