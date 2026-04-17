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
/// Implements city operations with Redis caching and parent validation.
/// <para>See <see cref="ICityService"/>, <see cref="City"/>.</para>
/// </summary>
public sealed class CityService : BaseNomenclatureEntityService, ICityService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public CityService(NomenclatureDbContext context, IMapper mapper, IDistributedCache cache)
        : base(context, mapper)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<CityDto>> CreateAsync(
        CreateCityRequest request,
        CancellationToken cancellationToken)
    {
        Result? parentValidation = await ValidateStateProvinceExistsAndActiveAsync(request.StateProvinceId, cancellationToken).ConfigureAwait(false);
        if (parentValidation is not null)
            return Result<CityDto>.Failure(parentValidation.ErrorCode!, parentValidation.ErrorMessage!, parentValidation.StatusCode!.Value);

        Result? nameValidation = await ValidateUniqueNameAsync(request.StateProvinceId, request.Name, null, cancellationToken).ConfigureAwait(false);
        if (nameValidation is not null)
            return Result<CityDto>.Failure(nameValidation.ErrorCode!, nameValidation.ErrorMessage!, nameValidation.StatusCode!.Value);

        City city = new()
        {
            StateProvinceId = request.StateProvinceId,
            Name = request.Name,
            PostalCode = request.PostalCode,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.Cities.Add(city);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(BuildCacheKey(request.StateProvinceId), cancellationToken).ConfigureAwait(false);

        CityDto dto = Mapper.Map<CityDto>(city);
        return Result<CityDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CityDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        City? city = await Context.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (city is null)
            return Result<CityDto>.Failure("CITY_NOT_FOUND", "City not found.", 404);

        CityDto dto = Mapper.Map<CityDto>(city);
        return Result<CityDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CityDto>>> ListByStateProvinceAsync(
        int stateProvinceId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        bool stateProvinceExists = await Context.StateProvinces
            .AnyAsync(sp => sp.Id == stateProvinceId, cancellationToken)
            .ConfigureAwait(false);

        if (!stateProvinceExists)
            return Result<IReadOnlyList<CityDto>>.Failure("STATE_PROVINCE_NOT_FOUND", "State/province not found.", 404);

        string cacheKey = BuildCacheKey(stateProvinceId);
        IReadOnlyList<CityDto>? cached = await GetCachedListAsync<CityDto>(cacheKey, cancellationToken).ConfigureAwait(false);

        if (cached is not null)
        {
            IReadOnlyList<CityDto> filtered = includeInactive
                ? cached
                : cached.Where(c => c.IsActive).ToList();
            return Result<IReadOnlyList<CityDto>>.Success(filtered);
        }

        List<City> cities = await Context.Cities
            .AsNoTracking()
            .Where(c => c.StateProvinceId == stateProvinceId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CityDto> allDtos = Mapper.Map<IReadOnlyList<CityDto>>(cities);
        await SetCacheAsync(cacheKey, allDtos, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<CityDto> result = includeInactive
            ? allDtos
            : allDtos.Where(c => c.IsActive).ToList();

        return Result<IReadOnlyList<CityDto>>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<CityDto>> UpdateAsync(
        int id,
        UpdateCityRequest request,
        CancellationToken cancellationToken)
    {
        City? city = await Context.Cities
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (city is null)
            return Result<CityDto>.Failure("CITY_NOT_FOUND", "City not found.", 404);

        if (city.Name != request.Name)
        {
            Result? nameValidation = await ValidateUniqueNameAsync(city.StateProvinceId, request.Name, id, cancellationToken).ConfigureAwait(false);
            if (nameValidation is not null)
                return Result<CityDto>.Failure(nameValidation.ErrorCode!, nameValidation.ErrorMessage!, nameValidation.StatusCode!.Value);
        }

        city.Name = request.Name;
        city.PostalCode = request.PostalCode;

        if (request.IsActive.HasValue)
            city.IsActive = request.IsActive.Value;

        city.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(BuildCacheKey(city.StateProvinceId), cancellationToken).ConfigureAwait(false);

        CityDto dto = Mapper.Map<CityDto>(city);
        return Result<CityDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CityDto>> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        City? city = await Context.Cities
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (city is null)
            return Result<CityDto>.Failure("CITY_NOT_FOUND", "City not found.", 404);

        if (!city.IsActive)
            return Result<CityDto>.Failure("CITY_ALREADY_INACTIVE", "City is already inactive.", 409);

        city.IsActive = false;
        city.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(BuildCacheKey(city.StateProvinceId), cancellationToken).ConfigureAwait(false);

        CityDto dto = Mapper.Map<CityDto>(city);
        return Result<CityDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CityDto>> ReactivateAsync(int id, CancellationToken cancellationToken)
    {
        City? city = await Context.Cities
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (city is null)
            return Result<CityDto>.Failure("CITY_NOT_FOUND", "City not found.", 404);

        if (city.IsActive)
            return Result<CityDto>.Failure("CITY_ALREADY_ACTIVE", "City is already active.", 409);

        Result? parentValidation = await ValidateParentStateProvinceActiveAsync(city.StateProvinceId, cancellationToken).ConfigureAwait(false);
        if (parentValidation is not null)
            return Result<CityDto>.Failure(parentValidation.ErrorCode!, parentValidation.ErrorMessage!, parentValidation.StatusCode!.Value);

        city.IsActive = true;
        city.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(BuildCacheKey(city.StateProvinceId), cancellationToken).ConfigureAwait(false);

        CityDto dto = Mapper.Map<CityDto>(city);
        return Result<CityDto>.Success(dto);
    }

    /// <summary>
    /// Builds the cache key for cities within a state/province.
    /// </summary>
    private static string BuildCacheKey(int stateProvinceId)
    {
        return $"nomenclature:cities:state-province:{stateProvinceId}";
    }

    /// <summary>
    /// Validates that the state/province exists and is active.
    /// </summary>
    private async Task<Result?> ValidateStateProvinceExistsAndActiveAsync(
        int stateProvinceId,
        CancellationToken cancellationToken)
    {
        StateProvince? stateProvince = await Context.StateProvinces
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.Id == stateProvinceId, cancellationToken)
            .ConfigureAwait(false);

        if (stateProvince is null)
            return Result.Failure("STATE_PROVINCE_NOT_FOUND", "State/province not found.", 404);

        if (!stateProvince.IsActive)
            return Result.Failure("INACTIVE_PARENT_STATE_PROVINCE", "Cannot create city in an inactive state/province.", 409);

        return null;
    }

    /// <summary>
    /// Validates that the parent state/province is active for reactivation.
    /// </summary>
    private async Task<Result?> ValidateParentStateProvinceActiveAsync(
        int stateProvinceId,
        CancellationToken cancellationToken)
    {
        bool isActive = await Context.StateProvinces
            .AnyAsync(sp => sp.Id == stateProvinceId && sp.IsActive, cancellationToken)
            .ConfigureAwait(false);

        return isActive
            ? null
            : Result.Failure("INACTIVE_PARENT_STATE_PROVINCE", "Cannot reactivate city because the parent state/province is inactive.", 409);
    }

    /// <summary>
    /// Validates that the city name is unique within the state/province.
    /// </summary>
    private async Task<Result?> ValidateUniqueNameAsync(
        int stateProvinceId,
        string name,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<City> query = Context.Cities
            .Where(c => c.StateProvinceId == stateProvinceId && c.Name == name);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_CITY_NAME", "A city with this name already exists in this state/province.", 409)
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
