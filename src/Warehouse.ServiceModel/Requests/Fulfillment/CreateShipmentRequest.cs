namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for creating a shipment (dispatching a packed sales order).
/// </summary>
public sealed record CreateShipmentRequest
{
    /// <summary>Gets the sales order ID. Required.</summary>
    public required int SalesOrderId { get; init; }

    /// <summary>Gets the optional carrier ID.</summary>
    public int? CarrierId { get; init; }

    /// <summary>Gets the optional carrier service level ID.</summary>
    public int? CarrierServiceLevelId { get; init; }

    /// <summary>Gets the optional notes. Max 2000 characters.</summary>
    public string? Notes { get; init; }
}
