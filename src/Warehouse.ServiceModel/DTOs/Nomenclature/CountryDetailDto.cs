namespace Warehouse.ServiceModel.DTOs.Nomenclature;

/// <summary>
/// Represents a country with its state/provinces for detail view.
/// </summary>
public sealed record CountryDetailDto
{
    /// <summary>
    /// Gets the country ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the ISO 3166-1 alpha-2 code.
    /// </summary>
    public required string Iso2Code { get; init; }

    /// <summary>
    /// Gets the ISO 3166-1 alpha-3 code.
    /// </summary>
    public required string Iso3Code { get; init; }

    /// <summary>
    /// Gets the country name in English.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the international dialing code.
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

    /// <summary>
    /// Gets the state/provinces belonging to this country.
    /// </summary>
    public required IReadOnlyList<StateProvinceDto> StateProvinces { get; init; }
}
