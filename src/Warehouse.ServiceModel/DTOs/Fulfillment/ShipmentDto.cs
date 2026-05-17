namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Lightweight shipment representation for list views.
/// </summary>
public sealed record ShipmentDto
{
    /// <summary>Gets the shipment ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the unique shipment number.</summary>
    public required string ShipmentNumber { get; init; }

    /// <summary>Gets the sales order ID.</summary>
    public required int SalesOrderId { get; init; }

    /// <summary>Gets the parent sales order number (mapped from SalesOrder.OrderNumber).</summary>
    public required string SalesOrderNumber { get; init; }

    /// <summary>Gets the optional carrier ID.</summary>
    public int? CarrierId { get; init; }

    /// <summary>Gets the optional carrier display name (from the local Fulfillment schema).</summary>
    public string? CarrierName { get; init; }

    /// <summary>Gets the current status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the optional tracking number.</summary>
    public string? TrackingNumber { get; init; }

    /// <summary>Gets the UTC dispatch timestamp.</summary>
    public required DateTime DispatchedAtUtc { get; init; }
}
