namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Shipment line DTO.
/// </summary>
public sealed record ShipmentLineDto
{
    /// <summary>Gets the shipment line ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the shipment ID.</summary>
    public required int ShipmentId { get; init; }

    /// <summary>Gets the sales order line ID.</summary>
    public required int SalesOrderLineId { get; init; }

    /// <summary>Gets the product ID.</summary>
    public required int ProductId { get; init; }

    /// <summary>Gets the optional product code resolved from the Inventory schema.</summary>
    public string? ProductCode { get; init; }

    /// <summary>Gets the optional product display name resolved from the Inventory schema.</summary>
    public string? ProductName { get; init; }

    /// <summary>Gets the shipped quantity (mapped from entity Quantity).</summary>
    public required decimal ShippedQuantity { get; init; }

    /// <summary>Gets the optional location ID.</summary>
    public int? LocationId { get; init; }

    /// <summary>Gets the optional batch ID.</summary>
    public int? BatchId { get; init; }
}
