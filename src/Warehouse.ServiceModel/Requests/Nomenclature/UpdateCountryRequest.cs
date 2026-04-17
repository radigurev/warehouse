namespace Warehouse.ServiceModel.Requests.Nomenclature;

/// <summary>
/// Request payload for updating an existing country. ISO codes are immutable after creation.
/// </summary>
public sealed record UpdateCountryRequest
{
    /// <summary>
    /// Gets the country name. Required, 1-100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the international dialing code. Optional, max 10 characters.
    /// </summary>
    public string? PhonePrefix { get; init; }

    /// <summary>
    /// Gets the active status flag.
    /// </summary>
    public bool? IsActive { get; init; }
}
