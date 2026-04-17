namespace Warehouse.Nomenclature.API.Seeding.Models;

/// <summary>
/// JSON deserialization model for country seed data.
/// </summary>
internal sealed record CountrySeedEntry
{
    /// <summary>
    /// Gets the ISO 3166-1 alpha-2 code.
    /// </summary>
    public required string Iso2 { get; init; }

    /// <summary>
    /// Gets the ISO 3166-1 alpha-3 code.
    /// </summary>
    public required string Iso3 { get; init; }

    /// <summary>
    /// Gets the country name in English.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the international dialing code.
    /// </summary>
    public string? Phone { get; init; }
}

/// <summary>
/// JSON deserialization model for currency seed data.
/// </summary>
internal sealed record CurrencySeedEntry
{
    /// <summary>
    /// Gets the ISO 4217 currency code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the currency name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the currency symbol.
    /// </summary>
    public string? Symbol { get; init; }
}

/// <summary>
/// JSON deserialization model for state/province seed data.
/// </summary>
internal sealed record StateProvinceSeedEntry
{
    /// <summary>
    /// Gets the parent country ISO 3166-1 alpha-2 code.
    /// </summary>
    public required string CountryIso2 { get; init; }

    /// <summary>
    /// Gets the subdivision code (ISO 3166-2 without country prefix).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the subdivision name in English.
    /// </summary>
    public required string Name { get; init; }
}

/// <summary>
/// JSON deserialization model for city seed data.
/// </summary>
internal sealed record CitySeedEntry
{
    /// <summary>
    /// Gets the parent state/province code.
    /// </summary>
    public required string StateCode { get; init; }

    /// <summary>
    /// Gets the city name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the postal/ZIP code.
    /// </summary>
    public string? PostalCode { get; init; }
}
