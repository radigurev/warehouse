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
/// Implements currency operations with Redis caching and soft-delete.
/// <para>See <see cref="ICurrencyService"/>, <see cref="Currency"/>.</para>
/// </summary>
public sealed class CurrencyService : BaseNomenclatureEntityService, ICurrencyService
{
    private const string CacheKey = "nomenclature:currencies:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public CurrencyService(NomenclatureDbContext context, IMapper mapper, IDistributedCache cache)
        : base(context, mapper)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<CurrencyDto>> CreateAsync(
        CreateCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        Result? codeValidation = await ValidateUniqueCodeAsync(request.Code, null, cancellationToken).ConfigureAwait(false);
        if (codeValidation is not null)
            return Result<CurrencyDto>.Failure(codeValidation.ErrorCode!, codeValidation.ErrorMessage!, codeValidation.StatusCode!.Value);

        Currency currency = new()
        {
            Code = request.Code,
            Name = request.Name,
            Symbol = request.Symbol,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.Currencies.Add(currency);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);

        CurrencyDto dto = Mapper.Map<CurrencyDto>(currency);
        return Result<CurrencyDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CurrencyDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Currency? currency = await Context.Currencies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (currency is null)
            return Result<CurrencyDto>.Failure("CURRENCY_NOT_FOUND", "Currency not found.", 404);

        CurrencyDto dto = Mapper.Map<CurrencyDto>(currency);
        return Result<CurrencyDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CurrencyDto>>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CurrencyDto>? cached = await GetCachedListAsync(cancellationToken).ConfigureAwait(false);

        if (cached is not null)
        {
            IReadOnlyList<CurrencyDto> filtered = includeInactive
                ? cached
                : cached.Where(c => c.IsActive).ToList();
            return Result<IReadOnlyList<CurrencyDto>>.Success(filtered);
        }

        List<Currency> currencies = await Context.Currencies
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CurrencyDto> allDtos = Mapper.Map<IReadOnlyList<CurrencyDto>>(currencies);
        await SetCacheAsync(allDtos, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<CurrencyDto> result = includeInactive
            ? allDtos
            : allDtos.Where(c => c.IsActive).ToList();

        return Result<IReadOnlyList<CurrencyDto>>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<CurrencyDto>> UpdateAsync(
        int id,
        UpdateCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        Currency? currency = await Context.Currencies
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (currency is null)
            return Result<CurrencyDto>.Failure("CURRENCY_NOT_FOUND", "Currency not found.", 404);

        currency.Name = request.Name;
        currency.Symbol = request.Symbol;

        if (request.IsActive.HasValue)
            currency.IsActive = request.IsActive.Value;

        currency.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);

        CurrencyDto dto = Mapper.Map<CurrencyDto>(currency);
        return Result<CurrencyDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CurrencyDto>> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        Currency? currency = await Context.Currencies
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (currency is null)
            return Result<CurrencyDto>.Failure("CURRENCY_NOT_FOUND", "Currency not found.", 404);

        if (!currency.IsActive)
            return Result<CurrencyDto>.Failure("CURRENCY_ALREADY_INACTIVE", "Currency is already inactive.", 409);

        currency.IsActive = false;
        currency.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);

        CurrencyDto dto = Mapper.Map<CurrencyDto>(currency);
        return Result<CurrencyDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CurrencyDto>> ReactivateAsync(int id, CancellationToken cancellationToken)
    {
        Currency? currency = await Context.Currencies
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (currency is null)
            return Result<CurrencyDto>.Failure("CURRENCY_NOT_FOUND", "Currency not found.", 404);

        if (currency.IsActive)
            return Result<CurrencyDto>.Failure("CURRENCY_ALREADY_ACTIVE", "Currency is already active.", 409);

        currency.IsActive = true;
        currency.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);

        CurrencyDto dto = Mapper.Map<CurrencyDto>(currency);
        return Result<CurrencyDto>.Success(dto);
    }

    /// <summary>
    /// Validates that the ISO 4217 currency code is unique.
    /// </summary>
    private async Task<Result?> ValidateUniqueCodeAsync(
        string code,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<Currency> query = Context.Currencies.Where(c => c.Code == code);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_CURRENCY_CODE", "A currency with this ISO 4217 code already exists.", 409)
            : null;
    }

    /// <summary>
    /// Attempts to read the full currency list from cache.
    /// </summary>
    private async Task<IReadOnlyList<CurrencyDto>?> GetCachedListAsync(CancellationToken cancellationToken)
    {
        try
        {
            byte[]? cached = await _cache.GetAsync(CacheKey, cancellationToken).ConfigureAwait(false);
            return cached is null ? null : JsonSerializer.Deserialize<List<CurrencyDto>>(cached);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Stores the full currency list in cache.
    /// </summary>
    private async Task SetCacheAsync(IReadOnlyList<CurrencyDto> items, CancellationToken cancellationToken)
    {
        try
        {
            byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(items);
            DistributedCacheEntryOptions options = new() { AbsoluteExpirationRelativeToNow = CacheDuration };
            await _cache.SetAsync(CacheKey, serialized, options, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    /// <summary>
    /// Removes the currency list from cache.
    /// </summary>
    private async Task InvalidateCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _cache.RemoveAsync(CacheKey, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
        }
    }
}
