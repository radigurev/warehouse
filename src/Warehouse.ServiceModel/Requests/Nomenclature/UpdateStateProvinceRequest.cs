namespace Warehouse.ServiceModel.Requests.Nomenclature;

/// <summary>
/// Request payload for updating an existing state/province. Code and country are immutable.
/// </summary>
public sealed record UpdateStateProvinceRequest
{
    /// <summary>
    /// Gets the state/province name. Required, 1-100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the active status flag.
    /// </summary>
    public bool? IsActive { get; init; }
}
