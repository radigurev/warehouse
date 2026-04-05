namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Lightweight zone representation for list views.
/// </summary>
public sealed record ZoneDto
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
}
