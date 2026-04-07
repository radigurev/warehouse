namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the status values for the shipment tracking lifecycle.
/// <para>Conforms to ISA-95 Part 3 -- Material Shipment activity.</para>
/// </summary>
public enum ShipmentStatus
{
    /// <summary>
    /// Shipment has been dispatched from the warehouse.
    /// </summary>
    Dispatched,

    /// <summary>
    /// Shipment is in transit to the destination.
    /// </summary>
    InTransit,

    /// <summary>
    /// Shipment is out for delivery to the final address.
    /// </summary>
    OutForDelivery,

    /// <summary>
    /// Shipment has been delivered to the customer. Terminal state.
    /// </summary>
    Delivered,

    /// <summary>
    /// Delivery attempt has failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Shipment has been returned after a failed delivery. Terminal state.
    /// </summary>
    Returned
}
