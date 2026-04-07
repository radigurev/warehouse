namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for updating sales order header fields (Draft only).
/// </summary>
public sealed record UpdateSalesOrderRequest
{
    /// <summary>Gets the ship-from warehouse ID. Required.</summary>
    public required int WarehouseId { get; init; }

    /// <summary>Gets the requested ship date. Optional.</summary>
    public DateOnly? RequestedShipDate { get; init; }

    /// <summary>Gets the shipping address street line 1. Required.</summary>
    public required string ShippingStreetLine1 { get; init; }

    /// <summary>Gets the shipping address street line 2. Optional.</summary>
    public string? ShippingStreetLine2 { get; init; }

    /// <summary>Gets the shipping address city. Required.</summary>
    public required string ShippingCity { get; init; }

    /// <summary>Gets the shipping address state/province. Optional.</summary>
    public string? ShippingStateProvince { get; init; }

    /// <summary>Gets the shipping address postal code. Required.</summary>
    public required string ShippingPostalCode { get; init; }

    /// <summary>Gets the shipping address country code (ISO 3166-1 alpha-2). Required.</summary>
    public required string ShippingCountryCode { get; init; }

    /// <summary>Gets the optional carrier ID.</summary>
    public int? CarrierId { get; init; }

    /// <summary>Gets the optional carrier service level ID.</summary>
    public int? CarrierServiceLevelId { get; init; }

    /// <summary>Gets the SO notes. Optional, max 2000 characters.</summary>
    public string? Notes { get; init; }
}
