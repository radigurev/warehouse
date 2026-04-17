using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Warehouse.Nomenclature.API.Seeding.Models;
using Warehouse.Nomenclature.DBModel;
using Warehouse.Nomenclature.DBModel.Models;

namespace Warehouse.Nomenclature.API.Seeding;

/// <summary>
/// Seeds reference data (countries, currencies, state/provinces, cities) into the nomenclature database
/// on startup by reading from embedded JSON resource files.
/// <para>Gated by feature flags: each seed method runs only when its flag is enabled.</para>
/// <para>See <see cref="NomenclatureDbContext"/>.</para>
/// </summary>
public sealed class NomenclatureSeeder
{
    /// <summary>
    /// Feature flag that gates the entire nomenclature seeding process.
    /// </summary>
    public const string EnableNomenclatureSeeding = "EnableNomenclatureSeeding";

    /// <summary>
    /// Feature flag that gates country reference data seeding.
    /// </summary>
    public const string EnableSeedCountries = "EnableSeedCountries";

    /// <summary>
    /// Feature flag that gates currency reference data seeding.
    /// </summary>
    public const string EnableSeedCurrencies = "EnableSeedCurrencies";

    /// <summary>
    /// Feature flag that gates state/province reference data seeding.
    /// </summary>
    public const string EnableSeedStateProvinces = "EnableSeedStateProvinces";

    /// <summary>
    /// Feature flag that gates city reference data seeding (Bulgaria only).
    /// </summary>
    public const string EnableSeedCities = "EnableSeedCities";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly NomenclatureDbContext _context;
    private readonly IFeatureManager _featureManager;
    private readonly ILogger<NomenclatureSeeder> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public NomenclatureSeeder(
        NomenclatureDbContext context,
        IFeatureManager featureManager,
        ILogger<NomenclatureSeeder> logger)
    {
        _context = context;
        _featureManager = featureManager;
        _logger = logger;
    }

    /// <summary>
    /// Seeds all reference data if the respective feature flags are enabled.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        bool seedingEnabled = await _featureManager.IsEnabledAsync(EnableNomenclatureSeeding).ConfigureAwait(false);
        if (!seedingEnabled)
        {
            _logger.LogInformation("Nomenclature seeding is disabled by feature flag {Flag}", EnableNomenclatureSeeding);
            return;
        }

        bool seedCountries = await _featureManager.IsEnabledAsync(EnableSeedCountries).ConfigureAwait(false);
        if (seedCountries)
            await SeedCountriesAsync(cancellationToken).ConfigureAwait(false);

        bool seedCurrencies = await _featureManager.IsEnabledAsync(EnableSeedCurrencies).ConfigureAwait(false);
        if (seedCurrencies)
            await SeedCurrenciesAsync(cancellationToken).ConfigureAwait(false);

        bool seedStateProvinces = await _featureManager.IsEnabledAsync(EnableSeedStateProvinces).ConfigureAwait(false);
        if (seedStateProvinces)
            await SeedStateProvincesAsync(cancellationToken).ConfigureAwait(false);

        bool seedCities = await _featureManager.IsEnabledAsync(EnableSeedCities).ConfigureAwait(false);
        if (seedCities)
            await SeedCitiesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Seeds all ISO 3166-1 countries from the embedded JSON resource.
    /// </summary>
    private async Task SeedCountriesAsync(CancellationToken cancellationToken)
    {
        List<CountrySeedEntry> entries = LoadEmbeddedJson<List<CountrySeedEntry>>("countries.json");

        HashSet<string> existingIso2Codes = (await _context.Countries
            .Select(c => c.Iso2Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<Country> countriesToInsert = entries
            .Where(e => !existingIso2Codes.Contains(e.Iso2))
            .Select(e => new Country
            {
                Iso2Code = e.Iso2,
                Iso3Code = e.Iso3,
                Name = e.Name,
                PhonePrefix = e.Phone,
                IsActive = true
            })
            .ToList();

        if (countriesToInsert.Count == 0)
        {
            _logger.LogInformation("No new countries to seed — all {Total} already exist", entries.Count);
            return;
        }

        _context.Countries.AddRange(countriesToInsert);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Seeded {Count} countries", countriesToInsert.Count);
    }

    /// <summary>
    /// Seeds all ISO 4217 currencies from the embedded JSON resource.
    /// </summary>
    private async Task SeedCurrenciesAsync(CancellationToken cancellationToken)
    {
        List<CurrencySeedEntry> entries = LoadEmbeddedJson<List<CurrencySeedEntry>>("currencies.json");

        HashSet<string> existingCodes = (await _context.Currencies
            .Select(c => c.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<Currency> currenciesToInsert = entries
            .Where(e => !existingCodes.Contains(e.Code))
            .Select(e => new Currency
            {
                Code = e.Code,
                Name = e.Name,
                Symbol = e.Symbol,
                IsActive = true
            })
            .ToList();

        if (currenciesToInsert.Count == 0)
        {
            _logger.LogInformation("No new currencies to seed — all {Total} already exist", entries.Count);
            return;
        }

        _context.Currencies.AddRange(currenciesToInsert);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Seeded {Count} currencies", currenciesToInsert.Count);
    }

    /// <summary>
    /// Seeds state/provinces from embedded JSON resources, linking to existing countries by ISO2 code.
    /// </summary>
    private async Task SeedStateProvincesAsync(CancellationToken cancellationToken)
    {
        List<StateProvinceSeedEntry> part1 = LoadEmbeddedJson<List<StateProvinceSeedEntry>>("state-provinces-part1.json");
        List<StateProvinceSeedEntry> part2 = LoadEmbeddedJson<List<StateProvinceSeedEntry>>("state-provinces-part2.json");
        List<StateProvinceSeedEntry> allEntries = [.. part1, .. part2];

        Dictionary<string, int> countryIdByIso2 = await _context.Countries
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Iso2Code, c => c.Id, cancellationToken)
            .ConfigureAwait(false);

        HashSet<string> existingKeys = (await _context.StateProvinces
            .AsNoTracking()
            .Select(sp => new { sp.CountryId, sp.Code })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .Select(x => $"{x.CountryId}:{x.Code}")
            .ToHashSet();

        List<StateProvince> toInsert = [];
        int skippedNoCountry = 0;

        foreach (StateProvinceSeedEntry entry in allEntries)
        {
            if (!countryIdByIso2.TryGetValue(entry.CountryIso2, out int countryId))
            {
                skippedNoCountry++;
                continue;
            }

            string key = $"{countryId}:{entry.Code}";
            if (existingKeys.Contains(key))
                continue;

            toInsert.Add(new StateProvince
            {
                CountryId = countryId,
                Code = entry.Code,
                Name = entry.Name,
                IsActive = true
            });

            existingKeys.Add(key);
        }

        if (toInsert.Count == 0)
        {
            _logger.LogInformation("No new state/provinces to seed — all already exist");
            return;
        }

        _context.StateProvinces.AddRange(toInsert);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Seeded {Count} state/provinces (skipped {Skipped} with no matching country)", toInsert.Count, skippedNoCountry);
    }

    /// <summary>
    /// Seeds Bulgarian cities from the embedded JSON resource, linking to existing state/provinces.
    /// </summary>
    private async Task SeedCitiesAsync(CancellationToken cancellationToken)
    {
        List<CitySeedEntry> entries = LoadEmbeddedJson<List<CitySeedEntry>>("cities-bg.json");

        Country? bulgaria = await _context.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Iso2Code == "BG", cancellationToken)
            .ConfigureAwait(false);

        if (bulgaria is null)
        {
            _logger.LogWarning("Cannot seed Bulgarian cities — country BG not found. Seed countries first.");
            return;
        }

        Dictionary<string, int> spIdByCode = await _context.StateProvinces
            .AsNoTracking()
            .Where(sp => sp.CountryId == bulgaria.Id)
            .ToDictionaryAsync(sp => sp.Code, sp => sp.Id, cancellationToken)
            .ConfigureAwait(false);

        HashSet<string> existingKeys = (await _context.Cities
            .AsNoTracking()
            .Where(c => c.StateProvince.CountryId == bulgaria.Id)
            .Select(c => new { c.StateProvinceId, c.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .Select(x => $"{x.StateProvinceId}:{x.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<City> toInsert = [];
        int skippedNoState = 0;

        foreach (CitySeedEntry entry in entries)
        {
            if (!spIdByCode.TryGetValue(entry.StateCode, out int stateProvinceId))
            {
                skippedNoState++;
                continue;
            }

            string key = $"{stateProvinceId}:{entry.Name}";
            if (existingKeys.Contains(key))
                continue;

            toInsert.Add(new City
            {
                StateProvinceId = stateProvinceId,
                Name = entry.Name,
                PostalCode = entry.PostalCode,
                IsActive = true
            });

            existingKeys.Add(key);
        }

        if (toInsert.Count == 0)
        {
            _logger.LogInformation("No new Bulgarian cities to seed — all already exist");
            return;
        }

        _context.Cities.AddRange(toInsert);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Seeded {Count} Bulgarian cities (skipped {Skipped} with no matching state/province)", toInsert.Count, skippedNoState);
    }

    /// <summary>
    /// Loads and deserializes an embedded JSON resource file from the Seeding/Data folder.
    /// </summary>
    private static T LoadEmbeddedJson<T>(string fileName)
    {
        Assembly assembly = typeof(NomenclatureSeeder).Assembly;
        string resourceName = $"Warehouse.Nomenclature.API.Seeding.Data.{fileName}";

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        return JsonSerializer.Deserialize<T>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize '{resourceName}'.");
    }
}
