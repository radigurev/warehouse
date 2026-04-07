using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Represents a shipment dispatched from a packed sales order.
/// <para>Conforms to ISA-95 Part 3 -- Material Shipment activity.</para>
/// <para>See <see cref="SalesOrder"/>, <see cref="ShipmentLine"/>, <see cref="ShipmentTracking"/>.</para>
/// </summary>
public sealed class Shipment : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique shipment number (format: SH-YYYYMMDD-NNNN).
    /// </summary>
    public required string ShipmentNumber { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the sales order (unique -- one shipment per SO in v1).
    /// </summary>
    public int SalesOrderId { get; set; }

    /// <summary>
    /// Gets or sets the optional carrier ID (FK to fulfillment.Carriers).
    /// </summary>
    public int? CarrierId { get; set; }

    /// <summary>
    /// Gets or sets the optional carrier service level ID (FK to fulfillment.CarrierServiceLevels).
    /// </summary>
    public int? CarrierServiceLevelId { get; set; }

    /// <summary>
    /// Gets or sets the shipment status (stored as nvarchar(30)).
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the shipping address street line 1 (copied from SO).
    /// </summary>
    public required string ShippingStreetLine1 { get; set; }

    /// <summary>
    /// Gets or sets the shipping address street line 2.
    /// </summary>
    public string? ShippingStreetLine2 { get; set; }

    /// <summary>
    /// Gets or sets the shipping address city.
    /// </summary>
    public required string ShippingCity { get; set; }

    /// <summary>
    /// Gets or sets the shipping address state/province.
    /// </summary>
    public string? ShippingStateProvince { get; set; }

    /// <summary>
    /// Gets or sets the shipping address postal code.
    /// </summary>
    public required string ShippingPostalCode { get; set; }

    /// <summary>
    /// Gets or sets the shipping address country code.
    /// </summary>
    public required string ShippingCountryCode { get; set; }

    /// <summary>
    /// Gets or sets the optional tracking number (max 100 characters).
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gets or sets the optional tracking URL (max 500 characters).
    /// </summary>
    public string? TrackingUrl { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the shipment was dispatched.
    /// </summary>
    public DateTime DispatchedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who dispatched this shipment (cross-schema ref to auth.Users).
    /// </summary>
    public int DispatchedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the sales order.
    /// </summary>
    public SalesOrder SalesOrder { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the carrier.
    /// </summary>
    public Carrier? Carrier { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the carrier service level.
    /// </summary>
    public CarrierServiceLevel? CarrierServiceLevel { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of shipment lines.
    /// </summary>
    public ICollection<ShipmentLine> Lines { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of tracking entries.
    /// </summary>
    public ICollection<ShipmentTracking> TrackingEntries { get; set; } = [];
}
