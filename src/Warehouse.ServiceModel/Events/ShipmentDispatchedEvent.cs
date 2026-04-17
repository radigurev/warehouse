namespace Warehouse.ServiceModel.Events;

/// <summary>
/// Published when a shipment is dispatched from a packed sales order.
/// Event naming: Fulfillment.Shipment.Dispatched.
/// </summary>
public sealed record ShipmentDispatchedEvent : ICorrelatedEvent
{
    /// <summary>
    /// Gets the shipment ID.
    /// </summary>
    public required int ShipmentId { get; init; }

    /// <summary>
    /// Gets the shipment number.
    /// </summary>
    public required string ShipmentNumber { get; init; }

    /// <summary>
    /// Gets the sales order ID.
    /// </summary>
    public required int SalesOrderId { get; init; }

    /// <summary>
    /// Gets the sales order number.
    /// </summary>
    public required string SalesOrderNumber { get; init; }

    /// <summary>
    /// Gets the warehouse ID from which items were shipped.
    /// </summary>
    public required int WarehouseId { get; init; }

    /// <summary>
    /// Gets the user who dispatched the shipment.
    /// </summary>
    public required int DispatchedByUserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the shipment was dispatched.
    /// </summary>
    public required DateTime DispatchedAtUtc { get; init; }

    /// <summary>
    /// Gets the collection of shipped lines.
    /// </summary>
    public required IReadOnlyList<ShipmentDispatchedLine> Lines { get; init; }

    /// <summary>
    /// Gets the correlation ID from the originating HTTP request.
    /// </summary>
    public string? CorrelationId { get; set; }
}
