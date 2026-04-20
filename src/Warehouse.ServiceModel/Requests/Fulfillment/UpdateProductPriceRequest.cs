namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for updating an existing product price entry.
/// ProductId and CurrencyCode are immutable after creation (CHG-FEAT-007 §2.2 and V7).
/// </summary>
public sealed record UpdateProductPriceRequest
{
    /// <summary>Gets the unit price excl. tax. Required, must be &gt;= 0.</summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>Gets the inclusive validity start (UTC). Optional.</summary>
    public DateTime? ValidFrom { get; init; }

    /// <summary>Gets the exclusive validity end (UTC). Optional; must be &gt; ValidFrom when both supplied.</summary>
    public DateTime? ValidTo { get; init; }
}
