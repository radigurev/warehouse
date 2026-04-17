namespace Warehouse.Infrastructure.Caching;

/// <summary>
/// Read-only resolver that looks up country and currency names from the shared
/// Nomenclature Redis cache. All methods are fail-open: they return <c>null</c>
/// or <c>false</c> when the cache is unavailable, empty, or the code is unknown.
/// <para>Consumer services (Customers, Purchasing, Fulfillment) register this as
/// a singleton and use it for DTO enrichment and optional backend validation.
/// See CHG-ENH-001.</para>
/// </summary>
public interface INomenclatureResolver
{
    /// <summary>
    /// Returns <c>true</c> if the ISO 3166-1 alpha-2 code matches an active country
    /// in the Nomenclature cache; <c>false</c> otherwise (including cache miss).
    /// </summary>
    Task<bool> IsValidCountryCodeAsync(string iso2Code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an ISO 3166-1 alpha-2 code to the country's English display name.
    /// Returns <c>null</c> on cache miss, unknown code, or Redis failure.
    /// </summary>
    Task<string?> ResolveCountryNameAsync(string iso2Code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> if the ISO 4217 code matches an active currency
    /// in the Nomenclature cache; <c>false</c> otherwise (including cache miss).
    /// </summary>
    Task<bool> IsValidCurrencyCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an ISO 4217 code to the currency's display name.
    /// Returns <c>null</c> on cache miss, unknown code, or Redis failure.
    /// </summary>
    Task<string?> ResolveCurrencyNameAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an ISO 3166-1 alpha-2 country code with tri-state return for fail-open support.
    /// Returns <c>true</c> if valid, <c>false</c> if invalid (cache available, code not found),
    /// <c>null</c> if cache is unavailable.
    /// </summary>
    Task<bool?> ValidateCountryCodeAsync(string iso2Code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an ISO 4217 currency code with tri-state return for fail-open support.
    /// Returns <c>true</c> if valid, <c>false</c> if invalid (cache available, code not found),
    /// <c>null</c> if cache is unavailable.
    /// </summary>
    Task<bool?> ValidateCurrencyCodeAsync(string code, CancellationToken cancellationToken = default);
}
