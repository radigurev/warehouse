namespace Warehouse.ServiceModel.Requests.Nomenclature;

/// <summary>
/// Request payload for creating a new currency.
/// </summary>
public sealed record CreateCurrencyRequest
{
    /// <summary>
    /// Gets the ISO 4217 currency code. Required, exactly 3 uppercase letters.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the currency name. Required, 1-100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the currency symbol. Optional, max 5 characters.
    /// </summary>
    public string? Symbol { get; init; }
}
