namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Full sales order representation including lines, progress, and shipping details.
/// </summary>
public sealed record SalesOrderDetailDto
{
    /// <summary>Gets the sales order ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the unique SO number.</summary>
    public required string OrderNumber { get; init; }

    /// <summary>Gets the customer ID.</summary>
    public required int CustomerId { get; init; }

    /// <summary>Gets the current status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the ship-from warehouse ID.</summary>
    public required int WarehouseId { get; init; }

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

    /// <summary>Gets the optional carrier ID.</summary>
    public int? CarrierId { get; init; }

    /// <summary>Gets the optional carrier service level ID.</summary>
    public int? CarrierServiceLevelId { get; init; }

    /// <summary>Gets the optional notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Gets the total amount.</summary>
    public required decimal TotalAmount { get; init; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Gets the UTC confirmation timestamp.</summary>
    public DateTime? ConfirmedAtUtc { get; init; }

    /// <summary>Gets the UTC shipped timestamp.</summary>
    public DateTime? ShippedAtUtc { get; init; }

    /// <summary>Gets the UTC completed timestamp.</summary>
    public DateTime? CompletedAtUtc { get; init; }

    /// <summary>Gets the collection of SO lines with progress.</summary>
    public required IReadOnlyList<SalesOrderLineDto> Lines { get; init; }
}
