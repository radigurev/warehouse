namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Storage location representation for list and detail views.
/// </summary>
public sealed record StorageLocationDto
{
    /// <summary>
    /// Gets the storage location ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the location code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the location name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the parent warehouse ID.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the parent warehouse name.
    /// </summary>
    public required string WarehouseName { get; init; }

    /// <summary>
    /// Gets the optional zone ID.
    /// </summary>
    public int? ZoneId { get; init; }

    /// <summary>
    /// Gets the optional zone name.
    /// </summary>
    public string? ZoneName { get; init; }

    /// <summary>
    /// Gets the location type (Row, Shelf, Bin, Bulk).
    /// </summary>
    public required string LocationType { get; init; }

    /// <summary>
    /// Gets the optional capacity.
    /// </summary>
    public decimal? Capacity { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
