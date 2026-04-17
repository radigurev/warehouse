namespace Warehouse.ServiceModel.DTOs.Customers;

/// <summary>
/// Represents a customer address with type and location details.
/// </summary>
public sealed record CustomerAddressDto
{
    /// <summary>
    /// Gets the address ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the address type (Billing, Shipping, or Both).
    /// </summary>
    public required string AddressType { get; init; }

    /// <summary>
    /// Gets the primary street line.
    /// </summary>
    public required string StreetLine1 { get; init; }

    /// <summary>
    /// Gets the secondary street line.
    /// </summary>
    public string? StreetLine2 { get; init; }

    /// <summary>
    /// Gets the city name.
    /// </summary>
    public required string City { get; init; }

    /// <summary>
    /// Gets the state or province name.
    /// </summary>
    public string? StateProvince { get; init; }

    /// <summary>
    /// Gets the postal code.
    /// </summary>
    public required string PostalCode { get; init; }

    /// <summary>
    /// Gets the ISO 3166-1 alpha-2 country code.
    /// </summary>
    public required string CountryCode { get; init; }

    /// <summary>
    /// Gets the country display name resolved from Nomenclature cache.
    /// Null when the cache is unavailable or the code is unresolved.
    /// </summary>
    public string? CountryName { get; init; }

    /// <summary>
    /// Gets whether this is the default address for its type.
    /// </summary>
    public required bool IsDefault { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
