using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Warehouse.Common.Models;
using Warehouse.Nomenclature.API.Interfaces;
using Warehouse.Nomenclature.DBModel;
using Warehouse.Nomenclature.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Nomenclature;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Services;

/// <summary>
/// Implements country operations with Redis caching and cascade deactivation.
/// <para>See <see cref="ICountryService"/>, <see cref="Country"/>.</para>
/// </summary>
public sealed class CountryService : BaseNomenclatureEntityService, ICountryService
{
    private const string CacheKey = "nomenclature:countries:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public CountryService(NomenclatureDbContext context, IMapper mapper, IDistributedCache cache)
        : base(context, mapper)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<CountryDto>> CreateAsync(
        CreateCountryRequest request,
        CancellationToken cancellationToken)
    {
        Result? iso2Validation = await ValidateUniqueIso2CodeAsync(request.Iso2Code, null, cancellationToken).ConfigureAwait(false);
        if (iso2Validation is not null)
            return Result<CountryDto>.Failure(iso2Validation.ErrorCode!, iso2Validation.ErrorMessage!, iso2Validation.StatusCode!.Value);

        Result? iso3Validation = await ValidateUniqueIso3CodeAsync(request.Iso3Code, null, cancellationToken).ConfigureAwait(false);
        if (iso3Validation is not null)
            return Result<CountryDto>.Failure(iso3Validation.ErrorCode!, iso3Validation.ErrorMessage!, iso3Validation.StatusCode!.Value);

        Country country = new()
        {
            Iso2Code = request.Iso2Code,
            Iso3Code = request.Iso3Code,
            Name = request.Name,
            PhonePrefix = request.PhonePrefix,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.Countries.Add(country);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(CacheKey, cancellationToken).ConfigureAwait(false);

        CountryDto dto = Mapper.Map<CountryDto>(country);
        return Result<CountryDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CountryDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Country? country = await Context.Countries
            .AsNoTracking()
            .Include(c => c.StateProvinces)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (country is null)
            return Result<CountryDetailDto>.Failure("COUNTRY_NOT_FOUND", "Country not found.", 404);

        CountryDetailDto dto = Mapper.Map<CountryDetailDto>(country);
        return Result<CountryDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CountryDto>>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CountryDto>? cached = await GetCachedListAsync<CountryDto>(CacheKey, cancellationToken).ConfigureAwait(false);

        if (cached is not null)
        {
            IReadOnlyList<CountryDto> filtered = includeInactive
                ? cached
                : cached.Where(c => c.IsActive).ToList();
            return Result<IReadOnlyList<CountryDto>>.Success(filtered);
        }

        List<Country> countries = await Context.Countries
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CountryDto> allDtos = Mapper.Map<IReadOnlyList<CountryDto>>(countries);
        await SetCacheAsync(CacheKey, allDtos, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<CountryDto> result = includeInactive
            ? allDtos
            : allDtos.Where(c => c.IsActive).ToList();

        return Result<IReadOnlyList<CountryDto>>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<CountryDto>> UpdateAsync(
        int id,
        UpdateCountryRequest request,
        CancellationToken cancellationToken)
    {
        Country? country = await Context.Countries
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (country is null)
            return Result<CountryDto>.Failure("COUNTRY_NOT_FOUND", "Country not found.", 404);

        country.Name = request.Name;
        country.PhonePrefix = request.PhonePrefix;

        if (request.IsActive.HasValue)
            country.IsActive = request.IsActive.Value;

        country.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(CacheKey, cancellationToken).ConfigureAwait(false);

        CountryDto dto = Mapper.Map<CountryDto>(country);
        return Result<CountryDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CountryDto>> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        Country? country = await Context.Countries
            .Include(c => c.StateProvinces)
            .ThenInclude(sp => sp.Cities)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (country is null)
            return Result<CountryDto>.Failure("COUNTRY_NOT_FOUND", "Country not found.", 404);

        if (!country.IsActive)
            return Result<CountryDto>.Failure("COUNTRY_ALREADY_INACTIVE", "Country is already inactive.", 409);

        country.IsActive = false;
        country.ModifiedAtUtc = DateTime.UtcNow;

        CascadeDeactivateChildren(country);

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCascadeCachesAsync(country, cancellationToken).ConfigureAwait(false);

        CountryDto dto = Mapper.Map<CountryDto>(country);
        return Result<CountryDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CountryDto>> ReactivateAsync(int id, CancellationToken cancellationToken)
    {
        Country? country = await Context.Countries
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (country is null)
            return Result<CountryDto>.Failure("COUNTRY_NOT_FOUND", "Country not found.", 404);

        if (country.IsActive)
            return Result<CountryDto>.Failure("COUNTRY_ALREADY_ACTIVE", "Country is already active.", 409);

        country.IsActive = true;
        country.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(CacheKey, cancellationToken).ConfigureAwait(false);

        CountryDto dto = Mapper.Map<CountryDto>(country);
        return Result<CountryDto>.Success(dto);
    }

    /// <summary>
    /// Cascades deactivation to all state/provinces and their cities.
    /// </summary>
    private static void CascadeDeactivateChildren(Country country)
    {
        foreach (StateProvince stateProvince in country.StateProvinces)
        {
            stateProvince.IsActive = false;
            stateProvince.ModifiedAtUtc = DateTime.UtcNow;

            foreach (City city in stateProvince.Cities)
            {
                city.IsActive = false;
                city.ModifiedAtUtc = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Invalidates country cache and all affected child entity caches.
    /// </summary>
    private async Task InvalidateCascadeCachesAsync(Country country, CancellationToken cancellationToken)
    {
        await InvalidateCacheAsync(CacheKey, cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync($"nomenclature:state-provinces:country:{country.Id}", cancellationToken).ConfigureAwait(false);

        foreach (StateProvince stateProvince in country.StateProvinces)
        {
            await InvalidateCacheAsync($"nomenclature:cities:state-province:{stateProvince.Id}", cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Validates that the ISO 3166-1 alpha-2 code is unique.
    /// </summary>
    private async Task<Result?> ValidateUniqueIso2CodeAsync(
        string iso2Code,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<Country> query = Context.Countries.Where(c => c.Iso2Code == iso2Code);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_ISO2_CODE", "A country with this ISO 3166-1 alpha-2 code already exists.", 409)
            : null;
    }

    /// <summary>
    /// Validates that the ISO 3166-1 alpha-3 code is unique.
    /// </summary>
    private async Task<Result?> ValidateUniqueIso3CodeAsync(
        string iso3Code,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<Country> query = Context.Countries.Where(c => c.Iso3Code == iso3Code);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_ISO3_CODE", "A country with this ISO 3166-1 alpha-3 code already exists.", 409)
            : null;
    }

    /// <summary>
    /// Attempts to read a list from the distributed cache.
    /// </summary>
    private async Task<IReadOnlyList<TDto>?> GetCachedListAsync<TDto>(string key, CancellationToken cancellationToken)
    {
        try
        {
            byte[]? cached = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
            return cached is null ? null : JsonSerializer.Deserialize<List<TDto>>(cached);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Stores a list in the distributed cache.
    /// </summary>
    private async Task SetCacheAsync<TDto>(string key, IReadOnlyList<TDto> items, CancellationToken cancellationToken)
    {
        try
        {
            byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(items);
            DistributedCacheEntryOptions options = new() { AbsoluteExpirationRelativeToNow = CacheDuration };
            await _cache.SetAsync(key, serialized, options, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    /// <summary>
    /// Removes an entry from the distributed cache.
    /// </summary>
    private async Task InvalidateCacheAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
        }
    }
}
