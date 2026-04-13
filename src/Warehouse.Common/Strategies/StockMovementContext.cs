using Warehouse.Common.Enums;

namespace Warehouse.Common.Strategies;

/// <summary>
/// Contextual data for stock movement creation via a strategy.
/// </summary>
public sealed class StockMovementContext
{
    /// <summary>
    /// Gets the product ID for the movement.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the warehouse ID for the movement.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the optional storage location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the optional batch ID.
    /// </summary>
    public int? BatchId { get; init; }

    /// <summary>
    /// Gets the movement quantity (positive for inbound, negative for outbound).
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Gets the movement reason code.
    /// </summary>
    public required StockMovementReason ReasonCode { get; init; }

    /// <summary>
    /// Gets the optional reference entity ID.
    /// </summary>
    public int? ReferenceId { get; init; }

    /// <summary>
    /// Gets the optional reference type.
    /// </summary>
    public StockMovementReferenceType? ReferenceType { get; init; }

    /// <summary>
    /// Gets the optional reference document number.
    /// </summary>
    public string? ReferenceNumber { get; init; }

    /// <summary>
    /// Gets the optional notes.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets the ID of the user creating the movement.
    /// </summary>
    public required int CreatedByUserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp for the movement.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
