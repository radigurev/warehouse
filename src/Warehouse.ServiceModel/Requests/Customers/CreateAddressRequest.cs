namespace Warehouse.ServiceModel.Requests.Customers;

/// <summary>
/// Request payload for creating a new customer address.
/// </summary>
public sealed record CreateAddressRequest
{
    /// <summary>
    /// Gets the address type. Required. One of: Billing, Shipping, Both.
    /// </summary>
    public required string AddressType { get; init; }

    /// <summary>
    /// Gets the primary street line. Required, 1-200 characters.
    /// </summary>
    public required string StreetLine1 { get; init; }

    /// <summary>
    /// Gets the secondary street line. Optional, max 200 characters.
    /// </summary>
    public string? StreetLine2 { get; init; }

    /// <summary>
    /// Gets the city name. Required, 1-100 characters.
    /// </summary>
    public required string City { get; init; }

    /// <summary>
    /// Gets the state or province name. Optional, max 100 characters.
    /// </summary>
    public string? StateProvince { get; init; }

    /// <summary>
    /// Gets the postal code. Required, 1-20 characters.
    /// </summary>
    public required string PostalCode { get; init; }

    /// <summary>
    /// Gets the ISO 3166-1 alpha-2 country code. Required, 2-letter code.
    /// </summary>
    public required string CountryCode { get; init; }
}
