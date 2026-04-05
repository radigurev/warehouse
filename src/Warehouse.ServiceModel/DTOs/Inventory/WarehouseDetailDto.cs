namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Full warehouse representation including zones and notes.
/// </summary>
public sealed record WarehouseDetailDto
{
    /// <summary>
    /// Gets the warehouse ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the unique warehouse code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the warehouse name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional address.
    /// </summary>
    public string? Address { get; init; }

    /// <summary>
    /// Gets the optional notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets whether the warehouse is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the collection of zones within this warehouse.
    /// </summary>
    public required IReadOnlyList<ZoneDto> Zones { get; init; }
}
