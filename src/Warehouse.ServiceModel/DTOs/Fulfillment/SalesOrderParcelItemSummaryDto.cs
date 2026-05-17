namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Packed item within a parcel, embedded in the SalesOrderDetailDto response.
/// </summary>
public sealed record SalesOrderParcelItemSummaryDto
{
    /// <summary>Gets the parcel item ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the product ID.</summary>
    public required int ProductId { get; init; }

    /// <summary>Gets the optional product code resolved from the Inventory schema.</summary>
    public string? ProductCode { get; init; }

    /// <summary>Gets the optional product name resolved from the Inventory schema.</summary>
    public string? ProductName { get; init; }

    /// <summary>Gets the pick list line ID.</summary>
    public required int PickListLineId { get; init; }

    /// <summary>Gets the packed quantity.</summary>
    public required decimal Quantity { get; init; }
}
