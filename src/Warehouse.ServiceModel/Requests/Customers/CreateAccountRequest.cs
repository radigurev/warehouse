namespace Warehouse.ServiceModel.Requests.Customers;

/// <summary>
/// Request payload for creating a new customer account.
/// </summary>
public sealed record CreateAccountRequest
{
    /// <summary>
    /// Gets the ISO 4217 currency code. Required, 3-letter code (e.g., USD, EUR, BGN).
    /// </summary>
    public required string CurrencyCode { get; init; }

    /// <summary>
    /// Gets the account description. Optional, max 500 characters.
    /// </summary>
    public string? Description { get; init; }
}
