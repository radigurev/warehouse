namespace Warehouse.ServiceModel.DTOs.Fulfillment;

/// <summary>
/// Represents a single product price entry in the Fulfillment-owned price catalog.
/// <para>Conforms to CHG-FEAT-007 §7 Detailed Design -- Request / Response DTOs.</para>
/// </summary>
public sealed record ProductPriceDto
{
    /// <summary>Gets the price ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the product ID (cross-schema ref to inventory.Products).</summary>
    public required int ProductId { get; init; }

    /// <summary>Gets the ISO 4217 3-letter currency code.</summary>
    public required string CurrencyCode { get; init; }

    /// <summary>Gets the unit price excl. tax.</summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>Gets the inclusive validity start (UTC); null means effective immediately.</summary>
    public DateTime? ValidFrom { get; init; }

    /// <summary>Gets the exclusive validity end (UTC); null means no end date.</summary>
    public DateTime? ValidTo { get; init; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Gets the ID of the user who created the price.</summary>
    public required int CreatedByUserId { get; init; }

    /// <summary>Gets the UTC last-modification timestamp, or null if never modified.</summary>
    public DateTime? ModifiedAtUtc { get; init; }

    /// <summary>Gets the ID of the user who last modified the price, or null if never modified.</summary>
    public int? ModifiedByUserId { get; init; }
}
