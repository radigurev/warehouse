namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for updating a sales order line (Draft only).
/// </summary>
public sealed record UpdateSalesOrderLineRequest
{
    /// <summary>Gets the ordered quantity. Required, must be greater than 0.</summary>
    public required decimal OrderedQuantity { get; init; }

    /// <summary>Gets the unit price. Required, must be 0 or greater.</summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>Gets the optional line notes. Max 500 characters.</summary>
    public string? Notes { get; init; }
}
