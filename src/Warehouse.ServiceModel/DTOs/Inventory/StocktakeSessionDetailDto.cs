namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Full stocktake session representation including counts.
/// </summary>
public sealed record StocktakeSessionDetailDto
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
    /// Gets the optional notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the ID of the user who created this session.
    /// </summary>
    public required int CreatedByUserId { get; init; }

    /// <summary>
    /// Gets the optional UTC start timestamp.
    /// </summary>
    public DateTime? StartedAtUtc { get; init; }

    /// <summary>
    /// Gets the optional UTC completion timestamp.
    /// </summary>
    public DateTime? CompletedAtUtc { get; init; }

    /// <summary>
    /// Gets the collection of count entries.
    /// </summary>
    public required IReadOnlyList<StocktakeCountDto> Counts { get; init; }
}
