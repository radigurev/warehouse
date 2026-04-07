namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Lightweight sales order representation for list views.
/// </summary>
public sealed record SalesOrderDto
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

    /// <summary>Gets the total amount.</summary>
    public required decimal TotalAmount { get; init; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public required DateTime CreatedAtUtc { get; init; }
}
