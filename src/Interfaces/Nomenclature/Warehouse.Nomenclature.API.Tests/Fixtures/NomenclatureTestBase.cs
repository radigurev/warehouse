using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;
using Warehouse.Mapping.Profiles.Nomenclature;
using Warehouse.Nomenclature.DBModel;
using Warehouse.Nomenclature.DBModel.Models;

namespace Warehouse.Nomenclature.API.Tests.Fixtures;

/// <summary>
/// Base class for nomenclature domain unit tests. Provides a fresh InMemory database and real AutoMapper per test.
/// </summary>
public abstract class NomenclatureTestBase
{
    /// <summary>
    /// Gets the InMemory EF Core context for the current test.
    /// </summary>
    protected NomenclatureDbContext Context { get; private set; } = null!;

    /// <summary>
    /// Gets the pre-configured AutoMapper mapper with all nomenclature mapping profiles.
    /// </summary>
    protected IMapper Mapper { get; private set; } = null!;

    [SetUp]
    public virtual void SetUp()
    {
        DbContextOptions<NomenclatureDbContext> options = new DbContextOptionsBuilder<NomenclatureDbContext>()
            .UseInMemoryDatabase(databaseName: $"NomenclatureTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        Context = new NomenclatureDbContext(options);

        MapperConfiguration config = new(cfg =>
        {
            cfg.AddProfile<NomenclatureMappingProfile>();
        });

        Mapper = config.CreateMapper();
    }

    [TearDown]
    public virtual void TearDown()
    {
        Context.Dispose();
    }

    /// <summary>
    /// Seeds an active country and returns the persisted entity.
    /// </summary>
    protected async Task<Country> SeedCountryAsync(
        string iso2Code = "BG",
        string iso3Code = "BGR",
        string name = "Bulgaria",
        string? phonePrefix = "+359",
        bool isActive = true)
    {
        Country country = new()
        {
            Iso2Code = iso2Code,
            Iso3Code = iso3Code,
            Name = name,
            PhonePrefix = phonePrefix,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.Countries.Add(country);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return country;
    }

    /// <summary>
    /// Seeds an active state/province and returns the persisted entity.
    /// </summary>
    protected async Task<StateProvince> SeedStateProvinceAsync(
        int countryId,
        string code = "SOF",
        string name = "Sofia Province",
        bool isActive = true)
    {
        StateProvince stateProvince = new()
        {
            CountryId = countryId,
            Code = code,
            Name = name,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.StateProvinces.Add(stateProvince);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return stateProvince;
    }

    /// <summary>
    /// Seeds an active city and returns the persisted entity.
    /// </summary>
    protected async Task<City> SeedCityAsync(
        int stateProvinceId,
        string name = "Sofia",
        string? postalCode = "1000",
        bool isActive = true)
    {
        City city = new()
        {
            StateProvinceId = stateProvinceId,
            Name = name,
            PostalCode = postalCode,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.Cities.Add(city);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return city;
    }

    /// <summary>
    /// Seeds an active currency and returns the persisted entity.
    /// </summary>
    protected async Task<Currency> SeedCurrencyAsync(
        string code = "BGN",
        string name = "Bulgarian Lev",
        string? symbol = "лв.",
        bool isActive = true)
    {
        Currency currency = new()
        {
            Code = code,
            Name = name,
            Symbol = symbol,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.Currencies.Add(currency);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return currency;
    }
}
