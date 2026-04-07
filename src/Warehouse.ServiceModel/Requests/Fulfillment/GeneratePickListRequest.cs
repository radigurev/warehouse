namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for generating a pick list from a confirmed sales order.
/// </summary>
public sealed record GeneratePickListRequest
{
    /// <summary>Gets the sales order ID. Required.</summary>
    public required int SalesOrderId { get; init; }
}
