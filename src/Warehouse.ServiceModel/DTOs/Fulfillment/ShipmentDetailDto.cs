namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Full shipment representation including lines, carrier, packed parcels, and tracking history.
/// </summary>
public sealed record ShipmentDetailDto
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

    /// <summary>Gets the optional carrier service level ID.</summary>
    public int? CarrierServiceLevelId { get; init; }

    /// <summary>Gets the optional carrier service level display name (from the local Fulfillment schema).</summary>
    public string? CarrierServiceLevelName { get; init; }

    /// <summary>Gets the current status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the shipping address street line 1.</summary>
    public required string ShippingStreetLine1 { get; init; }

    /// <summary>Gets the shipping address street line 2.</summary>
    public string? ShippingStreetLine2 { get; init; }

    /// <summary>Gets the shipping address city.</summary>
    public required string ShippingCity { get; init; }

    /// <summary>Gets the shipping address state/province.</summary>
    public string? ShippingStateProvince { get; init; }

    /// <summary>Gets the shipping address postal code.</summary>
    public required string ShippingPostalCode { get; init; }

    /// <summary>Gets the shipping address country code.</summary>
    public required string ShippingCountryCode { get; init; }

    /// <summary>Gets the shipping country display name resolved from Nomenclature cache. Null when unresolved.</summary>
    public string? ShippingCountryName { get; init; }

    /// <summary>Gets the optional tracking number.</summary>
    public string? TrackingNumber { get; init; }

    /// <summary>Gets the optional tracking URL.</summary>
    public string? TrackingUrl { get; init; }

    /// <summary>Gets the optional notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Gets the UTC dispatch timestamp.</summary>
    public required DateTime DispatchedAtUtc { get; init; }

    /// <summary>Gets the ID of the user who dispatched the shipment.</summary>
    public required int DispatchedByUserId { get; init; }

    /// <summary>Gets the collection of shipment lines.</summary>
    public required IReadOnlyList<ShipmentLineDto> Lines { get; init; }

    /// <summary>Gets the parcels packed into this shipment (loaded via sales-order scope).</summary>
    public required IReadOnlyList<SalesOrderParcelSummaryDto> Parcels { get; init; }

    /// <summary>Gets the collection of tracking entries recorded for this shipment (renamed from TrackingEntries).</summary>
    public required IReadOnlyList<ShipmentTrackingDto> TrackingHistory { get; init; }
}
