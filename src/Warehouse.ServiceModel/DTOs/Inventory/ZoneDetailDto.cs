namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Full zone representation including storage locations.
/// </summary>
public sealed record ZoneDetailDto
{
    /// <summary>
    /// Gets the zone ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the zone code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the zone name.
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
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the collection of storage locations within this zone.
    /// </summary>
    public required IReadOnlyList<StorageLocationDto> StorageLocations { get; init; }
}
