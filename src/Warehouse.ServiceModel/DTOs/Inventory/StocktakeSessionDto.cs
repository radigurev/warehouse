namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Stocktake session header representation for list views.
/// </summary>
public sealed record StocktakeSessionDto
{
    /// <summary>
    /// Gets the session ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the session name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the session status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the warehouse ID.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the warehouse name.
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
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the ID of the user who created this session.
    /// </summary>
    public required int CreatedByUserId { get; init; }
}
