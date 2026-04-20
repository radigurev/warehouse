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

    /// <summary>
    /// Gets the optional unit price override.
    /// When null, the unit price is resolved from the Product Price Catalog (CHG-FEAT-007 §2.3).
    /// When supplied, this value is preserved but catalog coverage is still required.
    /// Must be 0 or greater when provided.
    /// </summary>
    public decimal? UnitPrice { get; init; }

    /// <summary>Gets the optional line notes. Max 500 characters.</summary>
    public string? Notes { get; init; }
}
