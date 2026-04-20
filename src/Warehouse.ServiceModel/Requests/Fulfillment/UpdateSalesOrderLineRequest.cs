namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for updating a sales order line (Draft only).
/// </summary>
public sealed record UpdateSalesOrderLineRequest
{
    /// <summary>Gets the ordered quantity. Required, must be greater than 0.</summary>
    public required decimal OrderedQuantity { get; init; }

    /// <summary>
    /// Gets the optional unit price override.
    /// When null, the unit price is re-resolved from the Product Price Catalog (CHG-FEAT-007 §2.3).
    /// When supplied, this value is preserved but catalog coverage is still required.
    /// Must be 0 or greater when provided.
    /// </summary>
    public decimal? UnitPrice { get; init; }

    /// <summary>Gets the optional line notes. Max 500 characters.</summary>
    public string? Notes { get; init; }
}
