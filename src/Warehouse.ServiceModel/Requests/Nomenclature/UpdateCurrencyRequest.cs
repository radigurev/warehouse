namespace Warehouse.ServiceModel.Requests.Nomenclature;

/// <summary>
/// Request payload for updating an existing currency. Code is immutable.
/// </summary>
public sealed record UpdateCurrencyRequest
{
    /// <summary>
    /// Gets the currency name. Required, 1-100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the currency symbol. Optional, max 5 characters.
    /// </summary>
    public string? Symbol { get; init; }

    /// <summary>
    /// Gets the active status flag.
    /// </summary>
    public bool? IsActive { get; init; }
}
