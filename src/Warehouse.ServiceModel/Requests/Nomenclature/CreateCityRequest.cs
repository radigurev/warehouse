namespace Warehouse.ServiceModel.Requests.Nomenclature;

/// <summary>
/// Request payload for creating a new city.
/// </summary>
public sealed record CreateCityRequest
{
    /// <summary>
    /// Gets the parent state/province ID. Required, must be a positive integer.
    /// </summary>
    public required int StateProvinceId { get; init; }

    /// <summary>
    /// Gets the city name. Required, 1-100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional postal/ZIP code. Max 20 characters.
    /// </summary>
    public string? PostalCode { get; init; }
}
