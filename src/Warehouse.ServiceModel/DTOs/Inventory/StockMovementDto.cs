namespace Warehouse.ServiceModel.DTOs.Inventory;

/// <summary>
/// Stock movement record representation.
/// </summary>
public sealed record StockMovementDto
{
    /// <summary>
    /// Gets the movement ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the product ID.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the product name.
    /// </summary>
    public required string ProductName { get; init; }

    /// <summary>
    /// Gets the warehouse ID.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the warehouse name.
    /// </summary>
    public required string WarehouseName { get; init; }

    /// <summary>
    /// Gets the optional location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the optional location name.
    /// </summary>
    public string? LocationName { get; init; }

    /// <summary>
    /// Gets the movement quantity.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Gets the reason code for the movement.
    /// </summary>
    public required string ReasonCode { get; init; }

    /// <summary>
    /// Gets the optional reference number.
    /// </summary>
    public string? ReferenceNumber { get; init; }

    /// <summary>
    /// Gets the optional notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the ID of the user who created this movement.
    /// </summary>
    public required int CreatedByUserId { get; init; }
}
