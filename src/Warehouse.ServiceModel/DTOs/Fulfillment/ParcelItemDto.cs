namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Packed item within a parcel.
/// </summary>
public sealed record ParcelItemDto
{
    /// <summary>Gets the parcel item ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the parcel ID.</summary>
    public required int ParcelId { get; init; }

    /// <summary>Gets the pick list line ID.</summary>
    public required int PickListLineId { get; init; }

    /// <summary>Gets the product ID.</summary>
    public required int ProductId { get; init; }

    /// <summary>Gets the packed quantity.</summary>
    public required decimal Quantity { get; init; }
}
