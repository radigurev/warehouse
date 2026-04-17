namespace Warehouse.ServiceModel.DTOs.Nomenclature;

/// <summary>
/// Represents a city within a state/province for geographic reference.
/// </summary>
public sealed record CityDto
{
    /// <summary>
    /// Gets the city ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the parent state/province ID.
    /// </summary>
    public required int StateProvinceId { get; init; }

    /// <summary>
    /// Gets the city name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional postal/ZIP code.
    /// </summary>
    public string? PostalCode { get; init; }

    /// <summary>
    /// Gets whether the city is active.
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
