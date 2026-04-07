namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for confirming a pick list line as picked.
/// </summary>
public sealed record ConfirmPickRequest
{
    /// <summary>Gets the actual picked quantity. Required, must be greater than 0.</summary>
    public required decimal ActualQuantity { get; init; }
}
