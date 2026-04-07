using Warehouse.Common.Interfaces;

namespace Warehouse.Purchasing.DBModel.Models;

/// <summary>
/// Represents a physical address belonging to a supplier.
/// <para>See <see cref="Supplier"/>.</para>
/// </summary>
public sealed class SupplierAddress : IEntity
{
    /// <summary>
    /// Gets or sets the auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the owning supplier.
    /// </summary>
    public int SupplierId { get; set; }

    /// <summary>
    /// Gets or sets the address type (e.g., Billing, Shipping, Both).
    /// </summary>
    public required string AddressType { get; set; }

    /// <summary>
    /// Gets or sets the first street line (max 200 characters).
    /// </summary>
    public required string StreetLine1 { get; set; }

    /// <summary>
    /// Gets or sets the optional second street line (max 200 characters).
    /// </summary>
    public string? StreetLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city name (max 100 characters).
    /// </summary>
    public required string City { get; set; }

    /// <summary>
    /// Gets or sets the optional state or province (max 100 characters).
    /// </summary>
    public string? StateProvince { get; set; }

    /// <summary>
    /// Gets or sets the postal code (max 20 characters).
    /// </summary>
    public required string PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-2 country code (2 characters).
    /// </summary>
    public required string CountryCode { get; set; }

    /// <summary>
    /// Gets or sets whether this is the default address for its type.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC last-modification timestamp.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the owning supplier.
    /// </summary>
    public Supplier Supplier { get; set; } = null!;
}
