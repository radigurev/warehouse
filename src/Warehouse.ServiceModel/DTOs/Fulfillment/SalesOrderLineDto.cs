namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Sales order line DTO with pick/pack/ship progress tracking.
/// </summary>
public sealed record SalesOrderLineDto
{
    /// <summary>Gets the line ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the sales order ID.</summary>
    public required int SalesOrderId { get; init; }

    /// <summary>Gets the product ID.</summary>
    public required int ProductId { get; init; }

    /// <summary>Gets the ordered quantity.</summary>
    public required decimal OrderedQuantity { get; init; }

    /// <summary>Gets the unit price.</summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>Gets the line total.</summary>
    public required decimal LineTotal { get; init; }

    /// <summary>Gets the total picked quantity.</summary>
    public required decimal PickedQuantity { get; init; }

    /// <summary>Gets the total packed quantity.</summary>
    public required decimal PackedQuantity { get; init; }

    /// <summary>Gets the total shipped quantity.</summary>
    public required decimal ShippedQuantity { get; init; }

    /// <summary>Gets the optional notes.</summary>
    public string? Notes { get; init; }
}
