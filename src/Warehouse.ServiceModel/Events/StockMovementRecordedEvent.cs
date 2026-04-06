using Warehouse.Common.Enums;

namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published when a stock movement is successfully recorded.
/// </summary>
public sealed record StockMovementRecordedEvent
{
    /// <summary>
    /// Gets the database-assigned movement identifier.
    /// </summary>
    public required int MovementId { get; init; }

    /// <summary>
    /// Gets the product involved in the movement.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the warehouse where the movement occurred.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the signed quantity (positive = inbound, negative = outbound).
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Gets the ISA-95 aligned reason code for the movement.
    /// </summary>
    public required StockMovementReason ReasonCode { get; init; }

    /// <summary>
    /// Gets the user who recorded the movement.
    /// </summary>
    public required int CreatedByUserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the movement was recorded.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
