namespace Warehouse.ServiceModel.DTOs.Customers;

/// <summary>
/// Represents a customer account with currency and balance information.
/// </summary>
public sealed record CustomerAccountDto
{
    /// <summary>
    /// Gets the account ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the ISO 4217 currency code.
    /// </summary>
    public required string CurrencyCode { get; init; }

    /// <summary>
    /// Gets the currency display name resolved from Nomenclature cache.
    /// Null when the cache is unavailable or the code is unresolved.
    /// </summary>
    public string? CurrencyName { get; init; }

    /// <summary>
    /// Gets the current account balance.
    /// </summary>
    public required decimal Balance { get; init; }

    /// <summary>
    /// Gets the account description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets whether this is the primary account for the customer.
    /// </summary>
    public required bool IsPrimary { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
