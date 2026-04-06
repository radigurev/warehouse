using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Interfaces.Products;
using Warehouse.Inventory.API.Services.Base;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Services.Products;

/// <summary>
/// Implements unit of measure management operations with Redis caching.
/// <para>See <see cref="IUnitOfMeasureService"/>.</para>
/// </summary>
public sealed class UnitOfMeasureService : BaseInventoryEntityService, IUnitOfMeasureService
{
    private const string CacheKey = "inventory:units-of-measure:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public UnitOfMeasureService(InventoryDbContext context, IMapper mapper, IDistributedCache cache)
        : base(context, mapper)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<UnitOfMeasureDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        UnitOfMeasure? unit = await Context.UnitsOfMeasure
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (unit is null)
            return Result<UnitOfMeasureDto>.Failure("UNIT_NOT_FOUND", "Unit of measure not found.", 404);

        UnitOfMeasureDto dto = Mapper.Map<UnitOfMeasureDto>(unit);
        return Result<UnitOfMeasureDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<UnitOfMeasureDto>>> ListAsync(
        PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<UnitOfMeasureDto>? cached = await GetCachedListAsync(cancellationToken).ConfigureAwait(false);

        if (cached is not null)
            return PaginateFromList(cached, pagination);

        List<UnitOfMeasure> units = await Context.UnitsOfMeasure
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<UnitOfMeasureDto> allDtos = Mapper.Map<IReadOnlyList<UnitOfMeasureDto>>(units);
        await SetCacheAsync(allDtos, cancellationToken).ConfigureAwait(false);

        return PaginateFromList(allDtos, pagination);
    }

    /// <inheritdoc />
    public async Task<Result<UnitOfMeasureDto>> CreateAsync(
        CreateUnitOfMeasureRequest request,
        CancellationToken cancellationToken)
    {
        Result? codeValidation = await ValidateUniqueCodeAsync(request.Code, null, cancellationToken).ConfigureAwait(false);
        if (codeValidation is not null)
            return Result<UnitOfMeasureDto>.Failure(codeValidation.ErrorCode!, codeValidation.ErrorMessage!, codeValidation.StatusCode!.Value);

        UnitOfMeasure unit = new()
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.UnitsOfMeasure.Add(unit);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);

        UnitOfMeasureDto dto = Mapper.Map<UnitOfMeasureDto>(unit);
        return Result<UnitOfMeasureDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<UnitOfMeasureDto>> UpdateAsync(
        int id,
        UpdateUnitOfMeasureRequest request,
        CancellationToken cancellationToken)
    {
        UnitOfMeasure? unit = await Context.UnitsOfMeasure
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (unit is null)
            return Result<UnitOfMeasureDto>.Failure("UNIT_NOT_FOUND", "Unit of measure not found.", 404);

        unit.Name = request.Name;
        unit.Description = request.Description;
        unit.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);

        UnitOfMeasureDto dto = Mapper.Map<UnitOfMeasureDto>(unit);
        return Result<UnitOfMeasureDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        UnitOfMeasure? unit = await Context.UnitsOfMeasure
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (unit is null)
            return Result.Failure("UNIT_NOT_FOUND", "Unit of measure not found.", 404);

        int productCount = await Context.Products
            .CountAsync(p => p.UnitOfMeasureId == id && !p.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (productCount > 0)
            return Result.Failure("UNIT_IN_USE", $"Cannot delete unit -- it is assigned to {productCount} product(s).", 409);

        Context.UnitsOfMeasure.Remove(unit);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// Paginates an in-memory list of DTOs.
    /// </summary>
    private static Result<PaginatedResponse<UnitOfMeasureDto>> PaginateFromList(
        IReadOnlyList<UnitOfMeasureDto> items,
        PaginationParams pagination)
    {
        PaginatedResponse<UnitOfMeasureDto> response = new()
        {
            Items = items.Skip(pagination.Skip).Take(pagination.EffectivePageSize).ToList(),
            Page = pagination.Page,
            PageSize = pagination.EffectivePageSize,
            TotalCount = items.Count
        };

        return Result<PaginatedResponse<UnitOfMeasureDto>>.Success(response);
    }

    /// <summary>
    /// Attempts to read the full unit list from cache.
    /// </summary>
    private async Task<IReadOnlyList<UnitOfMeasureDto>?> GetCachedListAsync(CancellationToken cancellationToken)
    {
        byte[]? cached = await _cache.GetAsync(CacheKey, cancellationToken).ConfigureAwait(false);
        return cached is null ? null : JsonSerializer.Deserialize<List<UnitOfMeasureDto>>(cached);
    }

    /// <summary>
    /// Stores the full unit list in cache.
    /// </summary>
    private async Task SetCacheAsync(IReadOnlyList<UnitOfMeasureDto> items, CancellationToken cancellationToken)
    {
        byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(items);
        DistributedCacheEntryOptions options = new() { AbsoluteExpirationRelativeToNow = CacheDuration };
        await _cache.SetAsync(CacheKey, serialized, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the unit list from cache.
    /// </summary>
    private async Task InvalidateCacheAsync(CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(CacheKey, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates unit code uniqueness.
    /// </summary>
    private async Task<Result?> ValidateUniqueCodeAsync(
        string code,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<UnitOfMeasure> query = Context.UnitsOfMeasure.Where(u => u.Code == code);

        if (excludeId.HasValue)
            query = query.Where(u => u.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_UNIT_CODE", "A unit of measure with this code already exists.", 409)
            : null;
    }
}
