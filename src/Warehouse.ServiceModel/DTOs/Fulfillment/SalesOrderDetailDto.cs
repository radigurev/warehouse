namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Full sales order representation including lines, progress, shipping details,
/// nested pick lists / parcels / shipment, and cross-schema display names.
/// </summary>
public sealed record SalesOrderDetailDto
{
    /// <summary>Gets the sales order ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the unique SO number.</summary>
    public required string OrderNumber { get; init; }

    /// <summary>Gets the customer ID.</summary>
    public required int CustomerId { get; init; }

    /// <summary>Gets the customer display name resolved from the Customers schema. Null when unresolved.</summary>
    public string? CustomerName { get; init; }

    /// <summary>
    /// Gets the customer account ID cached on the SO header at creation.
    /// Used to supply the billing currency to the Product Price Catalog resolver.
    /// Added by CHG-FEAT-007 §2.9.
    /// </summary>
    public required int CustomerAccountId { get; init; }

    /// <summary>
    /// Gets the ISO 4217 currency code cached on the SO header at creation.
    /// Added by CHG-FEAT-007 §2.9.
    /// </summary>
    public required string CurrencyCode { get; init; }

    /// <summary>Gets the current status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the ship-from warehouse ID.</summary>
    public required int WarehouseId { get; init; }

    /// <summary>Gets the warehouse display name resolved from the Inventory schema. Null when unresolved.</summary>
    public string? WarehouseName { get; init; }

    /// <summary>Gets the requested ship date.</summary>
    public DateOnly? RequestedShipDate { get; init; }

    /// <summary>Gets shipping address street line 1.</summary>
    public required string ShippingStreetLine1 { get; init; }

    /// <summary>Gets shipping address street line 2.</summary>
    public string? ShippingStreetLine2 { get; init; }

    /// <summary>Gets shipping address city.</summary>
    public required string ShippingCity { get; init; }

    /// <summary>Gets shipping address state/province.</summary>
    public string? ShippingStateProvince { get; init; }

    /// <summary>Gets shipping address postal code.</summary>
    public required string ShippingPostalCode { get; init; }

    /// <summary>Gets shipping address country code.</summary>
    public required string ShippingCountryCode { get; init; }

    /// <summary>Gets the shipping country display name resolved from Nomenclature cache. Null when unresolved.</summary>
    public string? ShippingCountryName { get; init; }

    /// <summary>Gets billing address street line 1.</summary>
    public required string BillingStreetLine1 { get; init; }

    /// <summary>Gets billing address street line 2.</summary>
    public string? BillingStreetLine2 { get; init; }

    /// <summary>Gets billing address city.</summary>
    public required string BillingCity { get; init; }

    /// <summary>Gets billing address state/province.</summary>
    public string? BillingStateProvince { get; init; }

    /// <summary>Gets billing address postal code.</summary>
    public required string BillingPostalCode { get; init; }

    /// <summary>Gets billing address country code.</summary>
    public required string BillingCountryCode { get; init; }

    /// <summary>Gets the billing country display name resolved from Nomenclature cache. Null when unresolved.</summary>
    public string? BillingCountryName { get; init; }

    /// <summary>Gets the optional carrier ID.</summary>
    public int? CarrierId { get; init; }

    /// <summary>Gets the optional carrier display name (from the local Fulfillment schema).</summary>
    public string? CarrierName { get; init; }

    /// <summary>Gets the optional carrier service level ID.</summary>
    public int? CarrierServiceLevelId { get; init; }

    /// <summary>Gets the optional carrier service level display name (from the local Fulfillment schema).</summary>
    public string? CarrierServiceLevelName { get; init; }

    /// <summary>Gets the optional notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Gets the total amount.</summary>
    public required decimal TotalAmount { get; init; }

    /// <summary>Gets the ID of the user who created this sales order.</summary>
    public required int CreatedByUserId { get; init; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Gets the UTC confirmation timestamp.</summary>
    public DateTime? ConfirmedAtUtc { get; init; }

    /// <summary>Gets the UTC shipped timestamp.</summary>
    public DateTime? ShippedAtUtc { get; init; }

    /// <summary>Gets the UTC completed timestamp.</summary>
    public DateTime? CompletedAtUtc { get; init; }

    /// <summary>Gets the collection of SO lines with progress and product display names.</summary>
    public required IReadOnlyList<SalesOrderLineDto> Lines { get; init; }

    /// <summary>Gets the generated pick lists for this sales order.</summary>
    public required IReadOnlyList<SalesOrderPickListSummaryDto> PickLists { get; init; }

    /// <summary>Gets the parcels packed for this sales order.</summary>
    public required IReadOnlyList<SalesOrderParcelSummaryDto> Parcels { get; init; }

    /// <summary>Gets the dispatched shipment for this sales order, or null when not yet shipped.</summary>
    public SalesOrderShipmentSummaryDto? Shipment { get; init; }
}
