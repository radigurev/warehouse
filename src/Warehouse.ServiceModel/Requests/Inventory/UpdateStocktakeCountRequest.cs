namespace Warehouse.ServiceModel.Requests.Inventory;

/// <summary>
/// Request payload for updating a stocktake count entry's actual quantity.
/// </summary>
public sealed record UpdateStocktakeCountRequest
{
    /// <summary>
    /// Gets the updated actual counted quantity. Required.
    /// </summary>
    public required decimal CountedQuantity { get; init; }
}
