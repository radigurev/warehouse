using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Warehouse.ServiceModel.DTOs.Nomenclature;

namespace Warehouse.Infrastructure.Caching;

/// <summary>
/// Reads country and currency lists from the shared Nomenclature Redis cache.
/// Never writes to or invalidates cache keys — the Nomenclature service owns those.
/// All operations are fail-open: Redis unavailability or deserialization errors
/// result in <c>null</c> / <c>false</c> and are logged as warnings.
/// <para>See <see cref="INomenclatureResolver"/>, CHG-ENH-001.</para>
/// </summary>
public sealed class NomenclatureResolver : INomenclatureResolver
{
    private const string CountriesCacheKey = "nomenclature:countries:all";
    private const string CurrenciesCacheKey = "nomenclature:currencies:all";

    private readonly IDistributedCache _cache;
    private readonly ILogger<NomenclatureResolver> _logger;

    /// <summary>
    /// Initializes a new instance with the distributed cache and logger.
    /// </summary>
    public NomenclatureResolver(IDistributedCache cache, ILogger<NomenclatureResolver> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> IsValidCountryCodeAsync(string iso2Code, CancellationToken cancellationToken)
    {
        IReadOnlyList<CountryDto>? countries = await GetCountriesAsync(cancellationToken).ConfigureAwait(false);

        if (countries is null)
            return false;

        return countries.Any(c => c.IsActive
            && string.Equals(c.Iso2Code, iso2Code, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<string?> ResolveCountryNameAsync(string iso2Code, CancellationToken cancellationToken)
    {
        IReadOnlyList<CountryDto>? countries = await GetCountriesAsync(cancellationToken).ConfigureAwait(false);

        return countries?
            .FirstOrDefault(c => string.Equals(c.Iso2Code, iso2Code, StringComparison.OrdinalIgnoreCase))?
            .Name;
    }

    /// <inheritdoc />
    public async Task<bool> IsValidCurrencyCodeAsync(string code, CancellationToken cancellationToken)
    {
        IReadOnlyList<CurrencyDto>? currencies = await GetCurrenciesAsync(cancellationToken).ConfigureAwait(false);

        if (currencies is null)
            return false;

        return currencies.Any(c => c.IsActive
            && string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<string?> ResolveCurrencyNameAsync(string code, CancellationToken cancellationToken)
    {
        IReadOnlyList<CurrencyDto>? currencies = await GetCurrenciesAsync(cancellationToken).ConfigureAwait(false);

        return currencies?
            .FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase))?
            .Name;
    }

    /// <inheritdoc />
    public async Task<bool?> ValidateCountryCodeAsync(string iso2Code, CancellationToken cancellationToken)
    {
        IReadOnlyList<CountryDto>? countries = await GetCountriesAsync(cancellationToken).ConfigureAwait(false);

        if (countries is null)
            return null;

        return countries.Any(c => c.IsActive
            && string.Equals(c.Iso2Code, iso2Code, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<bool?> ValidateCurrencyCodeAsync(string code, CancellationToken cancellationToken)
    {
        IReadOnlyList<CurrencyDto>? currencies = await GetCurrenciesAsync(cancellationToken).ConfigureAwait(false);

        if (currencies is null)
            return null;

        return currencies.Any(c => c.IsActive
            && string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Reads the full country list from Redis. Returns null on any failure.
    /// </summary>
    private async Task<IReadOnlyList<CountryDto>?> GetCountriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            byte[]? cached = await _cache.GetAsync(CountriesCacheKey, cancellationToken).ConfigureAwait(false);

            if (cached is null)
                return null;

            return JsonSerializer.Deserialize<List<CountryDto>>(cached);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Nomenclature countries cache from key {CacheKey}", CountriesCacheKey);
            return null;
        }
    }

    /// <summary>
    /// Reads the full currency list from Redis. Returns null on any failure.
    /// </summary>
    private async Task<IReadOnlyList<CurrencyDto>?> GetCurrenciesAsync(CancellationToken cancellationToken)
    {
        try
        {
            byte[]? cached = await _cache.GetAsync(CurrenciesCacheKey, cancellationToken).ConfigureAwait(false);

            if (cached is null)
                return null;

            return JsonSerializer.Deserialize<List<CurrencyDto>>(cached);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Nomenclature currencies cache from key {CacheKey}", CurrenciesCacheKey);
            return null;
        }
    }
}
