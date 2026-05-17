namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Full pick list representation including all lines with pick status.
/// </summary>
public sealed record PickListDetailDto
{
    /// <summary>Gets the pick list ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the unique pick list number.</summary>
    public required string PickListNumber { get; init; }

    /// <summary>Gets the sales order ID.</summary>
    public required int SalesOrderId { get; init; }

    /// <summary>Gets the parent sales order number (mapped from SalesOrder.OrderNumber).</summary>
    public required string SalesOrderNumber { get; init; }

    /// <summary>Gets the ship-from warehouse ID (mapped from SalesOrder.WarehouseId).</summary>
    public required int WarehouseId { get; init; }

    /// <summary>Gets the optional warehouse display name resolved from the Inventory schema. Null when unresolved.</summary>
    public string? WarehouseName { get; init; }

    /// <summary>Gets the current status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Gets the ID of the user who created this pick list.</summary>
    public required int CreatedByUserId { get; init; }

    /// <summary>Gets the UTC completion timestamp.</summary>
    public DateTime? CompletedAtUtc { get; init; }

    /// <summary>Gets the collection of pick list lines.</summary>
    public required IReadOnlyList<PickListLineDto> Lines { get; init; }
}
