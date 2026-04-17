namespace Warehouse.ServiceModel.DTOs.Nomenclature;

/// <summary>
/// Represents a country for geographic reference (ISO 3166-1).
/// </summary>
public sealed record CountryDto
{
    /// <summary>
    /// Gets the country ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the ISO 3166-1 alpha-2 code (e.g., BG, US).
    /// </summary>
    public required string Iso2Code { get; init; }

    /// <summary>
    /// Gets the ISO 3166-1 alpha-3 code (e.g., BGR, USA).
    /// </summary>
    public required string Iso3Code { get; init; }

    /// <summary>
    /// Gets the country name in English.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the international dialing code (e.g., +359).
    /// </summary>
    public string? PhonePrefix { get; init; }

    /// <summary>
    /// Gets whether the country is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC last-modification timestamp.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; init; }
}
