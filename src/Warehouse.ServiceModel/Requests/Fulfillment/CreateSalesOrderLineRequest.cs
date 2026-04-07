namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for creating a sales order line.
/// </summary>
public sealed record CreateSalesOrderLineRequest
{
    /// <summary>Gets the product ID. Required.</summary>
    public required int ProductId { get; init; }

    /// <summary>Gets the ordered quantity. Required, must be greater than 0.</summary>
    public required decimal OrderedQuantity { get; init; }

    /// <summary>Gets the unit price. Required, must be 0 or greater.</summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>Gets the optional line notes. Max 500 characters.</summary>
    public string? Notes { get; init; }
}
