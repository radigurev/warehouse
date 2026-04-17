namespace Warehouse.ServiceModel.Requests.Nomenclature;

/// <summary>
/// Request payload for updating an existing city. State/province is immutable.
/// </summary>
public sealed record UpdateCityRequest
{
    /// <summary>
    /// Gets the city name. Required, 1-100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional postal/ZIP code. Max 20 characters.
    /// </summary>
    public string? PostalCode { get; init; }

    /// <summary>
    /// Gets the active status flag.
    /// </summary>
    public bool? IsActive { get; init; }
}
