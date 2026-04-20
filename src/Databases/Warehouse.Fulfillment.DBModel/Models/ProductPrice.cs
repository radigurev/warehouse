using Warehouse.Common.Interfaces;

namespace Warehouse.Fulfillment.DBModel.Models;

/// <summary>
/// Represents a product price entry in the Fulfillment-owned price catalog.
/// A price is keyed by (ProductId, CurrencyCode, ValidFrom) with optional validity window.
/// <para>Conforms to CHG-FEAT-007 §2.1 Catalog Data Model.</para>
/// <para>Classified as "Extended (non-ISA-95) WMS-owned commercial data" -- see CHG-FEAT-007 §1 ISA-95 deviation note.</para>
/// </summary>
public sealed class ProductPrice : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID (cross-schema plain FK to inventory.Products -- no EF navigation).
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the ISO 4217 3-letter currency code for this price.
    /// </summary>
    public required string CurrencyCode { get; set; }

    /// <summary>
    /// Gets or sets the unit price excl. tax with 4-decimal precision.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the inclusive validity start (UTC). Null means "effective immediately".
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// Gets or sets the exclusive validity end (UTC). Null means "no end date".
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this price (cross-schema ref to auth.Users).
    /// </summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional ID of the user who last modified this price.
    /// </summary>
    public int? ModifiedByUserId { get; set; }
}
