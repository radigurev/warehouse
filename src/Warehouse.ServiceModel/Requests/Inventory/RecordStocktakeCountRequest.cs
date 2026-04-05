namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for recording a stocktake count entry.
/// </summary>
public sealed record RecordStocktakeCountRequest
{
    /// <summary>
    /// Gets the product ID. Required.
    /// </summary>
    public required int ProductId { get; init; }

    /// <summary>
    /// Gets the optional location ID.
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Gets the actual counted quantity. Required.
    /// </summary>
    public required decimal CountedQuantity { get; init; }
}
