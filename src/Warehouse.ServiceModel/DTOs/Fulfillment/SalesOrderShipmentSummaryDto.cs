namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Compact shipment representation embedded in the SalesOrderDetailDto response.
/// </summary>
public sealed record SalesOrderShipmentSummaryDto
{
    /// <summary>Gets the shipment ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the unique shipment number.</summary>
    public required string ShipmentNumber { get; init; }

    /// <summary>Gets the current shipment status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the optional carrier ID.</summary>
    public int? CarrierId { get; init; }

    /// <summary>Gets the optional carrier display name (from the local Fulfillment schema).</summary>
    public string? CarrierName { get; init; }

    /// <summary>Gets the optional carrier service level ID.</summary>
    public int? CarrierServiceLevelId { get; init; }

    /// <summary>Gets the optional carrier service level display name (from the local Fulfillment schema).</summary>
    public string? CarrierServiceLevelName { get; init; }

    /// <summary>Gets the optional tracking number.</summary>
    public string? TrackingNumber { get; init; }

    /// <summary>Gets the optional tracking URL.</summary>
    public string? TrackingUrl { get; init; }

    /// <summary>Gets the optional dispatch notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Gets the count of shipment lines (distinct products on the shipment).</summary>
    public required int LineCount { get; init; }

    /// <summary>Gets the total shipped quantity summed across all shipment lines.</summary>
    public required decimal TotalQuantity { get; init; }

    /// <summary>Gets the ID of the user who dispatched this shipment.</summary>
    public required int DispatchedByUserId { get; init; }

    /// <summary>Gets the UTC dispatch timestamp.</summary>
    public required DateTime DispatchedAtUtc { get; init; }
}
