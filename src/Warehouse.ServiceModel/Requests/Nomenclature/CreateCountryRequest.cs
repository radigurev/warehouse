namespace Warehouse.ServiceModel.Requests.Nomenclature;

/// <summary>
/// Request payload for creating a new country.
/// </summary>
public sealed record CreateCountryRequest
{
    /// <summary>
    /// Gets the ISO 3166-1 alpha-2 code. Required, exactly 2 uppercase letters.
    /// </summary>
    public required string Iso2Code { get; init; }

    /// <summary>
    /// Gets the ISO 3166-1 alpha-3 code. Required, exactly 3 uppercase letters.
    /// </summary>
    public required string Iso3Code { get; init; }

    /// <summary>
    /// Gets the country name. Required, 1-100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the international dialing code. Optional, max 10 characters.
    /// </summary>
    public string? PhonePrefix { get; init; }
}
