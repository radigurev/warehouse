namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for adding an item to a parcel.
/// </summary>
public sealed record AddParcelItemRequest
{
    /// <summary>Gets the product ID. Required.</summary>
    public required int ProductId { get; init; }

    /// <summary>Gets the quantity to pack. Required, must be greater than 0.</summary>
    public required decimal Quantity { get; init; }

    /// <summary>Gets the source pick list line ID. Required.</summary>
    public required int PickListLineId { get; init; }
}
