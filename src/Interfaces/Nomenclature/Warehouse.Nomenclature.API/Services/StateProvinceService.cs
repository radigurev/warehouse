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
/// Implements state/province operations with Redis caching and cascade deactivation.
/// <para>See <see cref="IStateProvinceService"/>, <see cref="StateProvince"/>.</para>
/// </summary>
public sealed class StateProvinceService : BaseNomenclatureEntityService, IStateProvinceService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public StateProvinceService(NomenclatureDbContext context, IMapper mapper, IDistributedCache cache)
        : base(context, mapper)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<StateProvinceDto>> CreateAsync(
        CreateStateProvinceRequest request,
        CancellationToken cancellationToken)
    {
        Result? countryValidation = await ValidateCountryExistsAndActiveAsync(request.CountryId, cancellationToken).ConfigureAwait(false);
        if (countryValidation is not null)
            return Result<StateProvinceDto>.Failure(countryValidation.ErrorCode!, countryValidation.ErrorMessage!, countryValidation.StatusCode!.Value);

        Result? codeValidation = await ValidateUniqueCodeAsync(request.CountryId, request.Code, null, cancellationToken).ConfigureAwait(false);
        if (codeValidation is not null)
            return Result<StateProvinceDto>.Failure(codeValidation.ErrorCode!, codeValidation.ErrorMessage!, codeValidation.StatusCode!.Value);

        StateProvince stateProvince = new()
        {
            CountryId = request.CountryId,
            Code = request.Code,
            Name = request.Name,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.StateProvinces.Add(stateProvince);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(BuildCacheKey(request.CountryId), cancellationToken).ConfigureAwait(false);

        StateProvinceDto dto = Mapper.Map<StateProvinceDto>(stateProvince);
        return Result<StateProvinceDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<StateProvinceDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        StateProvince? stateProvince = await Context.StateProvinces
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (stateProvince is null)
            return Result<StateProvinceDto>.Failure("STATE_PROVINCE_NOT_FOUND", "State/province not found.", 404);

        StateProvinceDto dto = Mapper.Map<StateProvinceDto>(stateProvince);
        return Result<StateProvinceDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StateProvinceDto>>> ListByCountryAsync(
        int countryId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        bool countryExists = await Context.Countries
            .AnyAsync(c => c.Id == countryId, cancellationToken)
            .ConfigureAwait(false);

        if (!countryExists)
            return Result<IReadOnlyList<StateProvinceDto>>.Failure("COUNTRY_NOT_FOUND", "Country not found.", 404);

        string cacheKey = BuildCacheKey(countryId);
        IReadOnlyList<StateProvinceDto>? cached = await GetCachedListAsync<StateProvinceDto>(cacheKey, cancellationToken).ConfigureAwait(false);

        if (cached is not null)
        {
            IReadOnlyList<StateProvinceDto> filtered = includeInactive
                ? cached
                : cached.Where(sp => sp.IsActive).ToList();
            return Result<IReadOnlyList<StateProvinceDto>>.Success(filtered);
        }

        List<StateProvince> stateProvinces = await Context.StateProvinces
            .AsNoTracking()
            .Where(sp => sp.CountryId == countryId)
            .OrderBy(sp => sp.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<StateProvinceDto> allDtos = Mapper.Map<IReadOnlyList<StateProvinceDto>>(stateProvinces);
        await SetCacheAsync(cacheKey, allDtos, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<StateProvinceDto> result = includeInactive
            ? allDtos
            : allDtos.Where(sp => sp.IsActive).ToList();

        return Result<IReadOnlyList<StateProvinceDto>>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<StateProvinceDto>> UpdateAsync(
        int id,
        UpdateStateProvinceRequest request,
        CancellationToken cancellationToken)
    {
        StateProvince? stateProvince = await Context.StateProvinces
            .FirstOrDefaultAsync(sp => sp.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (stateProvince is null)
            return Result<StateProvinceDto>.Failure("STATE_PROVINCE_NOT_FOUND", "State/province not found.", 404);

        stateProvince.Name = request.Name;

        if (request.IsActive.HasValue)
            stateProvince.IsActive = request.IsActive.Value;

        stateProvince.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(BuildCacheKey(stateProvince.CountryId), cancellationToken).ConfigureAwait(false);

        StateProvinceDto dto = Mapper.Map<StateProvinceDto>(stateProvince);
        return Result<StateProvinceDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<StateProvinceDto>> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        StateProvince? stateProvince = await Context.StateProvinces
            .Include(sp => sp.Cities)
            .FirstOrDefaultAsync(sp => sp.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (stateProvince is null)
            return Result<StateProvinceDto>.Failure("STATE_PROVINCE_NOT_FOUND", "State/province not found.", 404);

        if (!stateProvince.IsActive)
            return Result<StateProvinceDto>.Failure("STATE_PROVINCE_ALREADY_INACTIVE", "State/province is already inactive.", 409);

        stateProvince.IsActive = false;
        stateProvince.ModifiedAtUtc = DateTime.UtcNow;

        CascadeDeactivateCities(stateProvince);

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateDeactivationCachesAsync(stateProvince, cancellationToken).ConfigureAwait(false);

        StateProvinceDto dto = Mapper.Map<StateProvinceDto>(stateProvince);
        return Result<StateProvinceDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<StateProvinceDto>> ReactivateAsync(int id, CancellationToken cancellationToken)
    {
        StateProvince? stateProvince = await Context.StateProvinces
            .FirstOrDefaultAsync(sp => sp.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (stateProvince is null)
            return Result<StateProvinceDto>.Failure("STATE_PROVINCE_NOT_FOUND", "State/province not found.", 404);

        if (stateProvince.IsActive)
            return Result<StateProvinceDto>.Failure("STATE_PROVINCE_ALREADY_ACTIVE", "State/province is already active.", 409);

        Result? parentValidation = await ValidateParentCountryActiveAsync(stateProvince.CountryId, cancellationToken).ConfigureAwait(false);
        if (parentValidation is not null)
            return Result<StateProvinceDto>.Failure(parentValidation.ErrorCode!, parentValidation.ErrorMessage!, parentValidation.StatusCode!.Value);

        stateProvince.IsActive = true;
        stateProvince.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(BuildCacheKey(stateProvince.CountryId), cancellationToken).ConfigureAwait(false);

        StateProvinceDto dto = Mapper.Map<StateProvinceDto>(stateProvince);
        return Result<StateProvinceDto>.Success(dto);
    }

    /// <summary>
    /// Builds the cache key for state/provinces within a country.
    /// </summary>
    private static string BuildCacheKey(int countryId)
    {
        return $"nomenclature:state-provinces:country:{countryId}";
    }

    /// <summary>
    /// Cascades deactivation to all cities within the state/province.
    /// </summary>
    private static void CascadeDeactivateCities(StateProvince stateProvince)
    {
        foreach (City city in stateProvince.Cities)
        {
            city.IsActive = false;
            city.ModifiedAtUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Invalidates the state/province cache for its country and the city caches for itself.
    /// </summary>
    private async Task InvalidateDeactivationCachesAsync(StateProvince stateProvince, CancellationToken cancellationToken)
    {
        await InvalidateCacheAsync(BuildCacheKey(stateProvince.CountryId), cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync($"nomenclature:cities:state-province:{stateProvince.Id}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates that the country exists and is active.
    /// </summary>
    private async Task<Result?> ValidateCountryExistsAndActiveAsync(
        int countryId,
        CancellationToken cancellationToken)
    {
        Country? country = await Context.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == countryId, cancellationToken)
            .ConfigureAwait(false);

        if (country is null)
            return Result.Failure("COUNTRY_NOT_FOUND", "Country not found.", 404);

        if (!country.IsActive)
            return Result.Failure("INACTIVE_PARENT_COUNTRY", "Cannot create state/province in an inactive country.", 409);

        return null;
    }

    /// <summary>
    /// Validates that the parent country is active for reactivation.
    /// </summary>
    private async Task<Result?> ValidateParentCountryActiveAsync(
        int countryId,
        CancellationToken cancellationToken)
    {
        bool isActive = await Context.Countries
            .AnyAsync(c => c.Id == countryId && c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        return isActive
            ? null
            : Result.Failure("INACTIVE_PARENT_COUNTRY", "Cannot reactivate state/province because the parent country is inactive.", 409);
    }

    /// <summary>
    /// Validates that the state/province code is unique within the country.
    /// </summary>
    private async Task<Result?> ValidateUniqueCodeAsync(
        int countryId,
        string code,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<StateProvince> query = Context.StateProvinces
            .Where(sp => sp.CountryId == countryId && sp.Code == code);

        if (excludeId.HasValue)
            query = query.Where(sp => sp.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_STATE_PROVINCE_CODE", "A state/province with this code already exists in this country.", 409)
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
