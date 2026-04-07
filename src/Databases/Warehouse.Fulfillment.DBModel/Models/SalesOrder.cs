using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Represents a sales order header with customer reference and status lifecycle.
/// <para>Conforms to ISA-95 Part 3 -- Fulfillment Operations Activity Model.</para>
/// <para>See <see cref="SalesOrderLine"/>, <see cref="PickList"/>, <see cref="Parcel"/>, <see cref="Shipment"/>.</para>
/// </summary>
public sealed class SalesOrder : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique SO number (format: SO-YYYYMMDD-NNNN).
    /// </summary>
    public required string OrderNumber { get; set; }

    /// <summary>
    /// Gets or sets the customer ID (cross-schema ref to customers.Customers -- no EF navigation).
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the SO status (stored as nvarchar(30)).
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the ship-from warehouse ID (cross-schema ref to inventory.Warehouses -- no EF navigation).
    /// </summary>
    public int WarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the requested ship date.
    /// </summary>
    public DateOnly? RequestedShipDate { get; set; }

    /// <summary>
    /// Gets or sets the shipping address street line 1 (max 200 characters).
    /// </summary>
    public required string ShippingStreetLine1 { get; set; }

    /// <summary>
    /// Gets or sets the shipping address street line 2 (max 200 characters).
    /// </summary>
    public string? ShippingStreetLine2 { get; set; }

    /// <summary>
    /// Gets or sets the shipping address city (max 100 characters).
    /// </summary>
    public required string ShippingCity { get; set; }

    /// <summary>
    /// Gets or sets the shipping address state/province (max 100 characters).
    /// </summary>
    public string? ShippingStateProvince { get; set; }

    /// <summary>
    /// Gets or sets the shipping address postal code (max 20 characters).
    /// </summary>
    public required string ShippingPostalCode { get; set; }

    /// <summary>
    /// Gets or sets the shipping address ISO 3166-1 alpha-2 country code (2 characters).
    /// </summary>
    public required string ShippingCountryCode { get; set; }

    /// <summary>
    /// Gets or sets the optional carrier ID (FK to fulfillment.Carriers).
    /// </summary>
    public int? CarrierId { get; set; }

    /// <summary>
    /// Gets or sets the optional carrier service level ID (FK to fulfillment.CarrierServiceLevels).
    /// </summary>
    public int? CarrierServiceLevelId { get; set; }

    /// <summary>
    /// Gets or sets the optional notes (max 2000 characters).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the total amount (computed from lines).
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this SO (cross-schema ref to auth.Users).
    /// </summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional ID of the user who last modified this SO.
    /// </summary>
    public int? ModifiedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the SO was confirmed.
    /// </summary>
    public DateTime? ConfirmedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who confirmed this SO.
    /// </summary>
    public int? ConfirmedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the SO was shipped.
    /// </summary>
    public DateTime? ShippedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the SO was completed.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who completed this SO.
    /// </summary>
    public int? CompletedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the carrier.
    /// </summary>
    public Carrier? Carrier { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the carrier service level.
    /// </summary>
    public CarrierServiceLevel? CarrierServiceLevel { get; set; }

    /// <summary>
    /// Gets or sets the navigation collection of SO lines.
    /// </summary>
    public ICollection<SalesOrderLine> Lines { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of pick lists for this SO.
    /// </summary>
    public ICollection<PickList> PickLists { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation collection of parcels for this SO.
    /// </summary>
    public ICollection<Parcel> Parcels { get; set; } = [];
}
