namespace Warehouse.ServiceModel.DTOs.Nomenclature;

/// <summary>
/// Represents a state/province within a country for geographic reference.
/// </summary>
public sealed record StateProvinceDto
{
    /// <summary>
    /// Gets the state/province ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the parent country ID.
    /// </summary>
    public required int CountryId { get; init; }

    /// <summary>
    /// Gets the state/province code (e.g., CA, SOF).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the state/province name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets whether the state/province is active.
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
