namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Pick list line DTO with pick execution details.
/// </summary>
public sealed record PickListLineDto
{
    /// <summary>Gets the line ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the pick list ID.</summary>
    public required int PickListId { get; init; }

    /// <summary>Gets the sales order line ID.</summary>
    public required int SalesOrderLineId { get; init; }

    /// <summary>Gets the product ID.</summary>
    public required int ProductId { get; init; }

    /// <summary>Gets the warehouse ID.</summary>
    public required int WarehouseId { get; init; }

    /// <summary>Gets the optional location ID.</summary>
    public int? LocationId { get; init; }

    /// <summary>Gets the requested pick quantity.</summary>
    public required decimal RequestedQuantity { get; init; }

    /// <summary>Gets the actual picked quantity (null if not yet picked).</summary>
    public decimal? ActualQuantity { get; init; }

    /// <summary>Gets the line pick status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the UTC timestamp when this line was picked.</summary>
    public DateTime? PickedAtUtc { get; init; }

    /// <summary>Gets the user who picked this line.</summary>
    public int? PickedByUserId { get; init; }
}
