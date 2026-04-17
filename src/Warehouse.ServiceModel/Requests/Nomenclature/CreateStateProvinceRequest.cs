namespace Warehouse.ServiceModel.Requests.Nomenclature;

/// <summary>
/// Request payload for creating a new state/province.
/// </summary>
public sealed record CreateStateProvinceRequest
{
    /// <summary>
    /// Gets the parent country ID. Required, must be a positive integer.
    /// </summary>
    public required int CountryId { get; init; }

    /// <summary>
    /// Gets the state/province code. Required, 1-10 characters.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the state/province name. Required, 1-100 characters.
    /// </summary>
    public required string Name { get; init; }
}
