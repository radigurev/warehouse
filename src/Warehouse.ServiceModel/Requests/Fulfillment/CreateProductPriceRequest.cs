namespace Warehouse.ServiceModel.Requests.Fulfillment;

/// <summary>
/// Request payload for creating a new product price catalog entry.
/// <para>Conforms to CHG-FEAT-007 §2.2 Catalog CRUD Behavior.</para>
/// </summary>
public sealed record CreateProductPriceRequest
{
    /// <summary>Gets the product ID. Required, must be &gt; 0.</summary>
    public required int ProductId { get; init; }

    /// <summary>Gets the ISO 4217 3-letter currency code (uppercase). Required.</summary>
    public required string CurrencyCode { get; init; }

    /// <summary>Gets the unit price excl. tax. Required, must be &gt;= 0.</summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>Gets the inclusive validity start (UTC). Optional.</summary>
    public DateTime? ValidFrom { get; init; }

    /// <summary>Gets the exclusive validity end (UTC). Optional; must be &gt; ValidFrom when both supplied.</summary>
    public DateTime? ValidTo { get; init; }
}
