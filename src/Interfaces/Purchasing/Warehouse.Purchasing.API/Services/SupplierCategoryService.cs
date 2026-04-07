using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Warehouse.Common.Models;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.Purchasing.API.Services.Base;
using Warehouse.Purchasing.DBModel;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Services;

/// <summary>
/// Implements supplier category management operations with distributed caching.
/// <para>See <see cref="ISupplierCategoryService"/>.</para>
/// </summary>
public sealed class SupplierCategoryService : BasePurchasingEntityService, ISupplierCategoryService
{
    private const string CacheKey = "purchasing:supplier-categories:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public SupplierCategoryService(PurchasingDbContext context, IMapper mapper, IDistributedCache cache)
        : base(context, mapper)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<SupplierCategoryDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        SupplierCategory? category = await Context.SupplierCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
            return Result<SupplierCategoryDto>.Failure("CATEGORY_NOT_FOUND", "Supplier category not found.", 404);

        return MapToResult<SupplierCategory, SupplierCategoryDto>(category);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<SupplierCategoryDto>>> GetAllAsync(
        PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SupplierCategoryDto>? cached = await GetCachedListAsync(cancellationToken).ConfigureAwait(false);

        if (cached is not null)
            return PaginateFromList(cached, pagination);

        List<SupplierCategory> categories = await Context.SupplierCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<SupplierCategoryDto> allDtos = Mapper.Map<IReadOnlyList<SupplierCategoryDto>>(categories);
        await SetCacheAsync(allDtos, cancellationToken).ConfigureAwait(false);

        return PaginateFromList(allDtos, pagination);
    }

    /// <inheritdoc />
    public async Task<Result<SupplierCategoryDto>> CreateAsync(
        CreateSupplierCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Result? nameValidation = await ValidateUniqueNameAsync(request.Name, null, cancellationToken).ConfigureAwait(false);
        if (nameValidation is not null)
            return Result<SupplierCategoryDto>.Failure(nameValidation.ErrorCode!, nameValidation.ErrorMessage!, nameValidation.StatusCode!.Value);

        SupplierCategory category = new()
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.SupplierCategories.Add(category);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);

        return MapToResult<SupplierCategory, SupplierCategoryDto>(category);
    }

    /// <inheritdoc />
    public async Task<Result<SupplierCategoryDto>> UpdateAsync(
        int id,
        UpdateSupplierCategoryRequest request,
        CancellationToken cancellationToken)
    {
        SupplierCategory? category = await Context.SupplierCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
            return Result<SupplierCategoryDto>.Failure("CATEGORY_NOT_FOUND", "Supplier category not found.", 404);

        Result? nameValidation = await ValidateUniqueNameAsync(request.Name, id, cancellationToken).ConfigureAwait(false);
        if (nameValidation is not null)
            return Result<SupplierCategoryDto>.Failure(nameValidation.ErrorCode!, nameValidation.ErrorMessage!, nameValidation.StatusCode!.Value);

        category.Name = request.Name;
        category.Description = request.Description;
        category.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);

        return MapToResult<SupplierCategory, SupplierCategoryDto>(category);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        SupplierCategory? category = await Context.SupplierCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
            return Result.Failure("CATEGORY_NOT_FOUND", "Supplier category not found.", 404);

        int supplierCount = await Context.Suppliers
            .CountAsync(s => s.CategoryId == id && !s.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (supplierCount > 0)
            return Result.Failure("CATEGORY_IN_USE", $"Cannot delete category -- it is assigned to {supplierCount} supplier(s).", 409);

        Context.SupplierCategories.Remove(category);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    private static Result<PaginatedResponse<SupplierCategoryDto>> PaginateFromList(
        IReadOnlyList<SupplierCategoryDto> items,
        PaginationParams pagination)
    {
        PaginatedResponse<SupplierCategoryDto> response = new()
        {
            Items = items.Skip(pagination.Skip).Take(pagination.EffectivePageSize).ToList(),
            Page = pagination.Page,
            PageSize = pagination.EffectivePageSize,
            TotalCount = items.Count
        };

        return Result<PaginatedResponse<SupplierCategoryDto>>.Success(response);
    }

    private async Task<IReadOnlyList<SupplierCategoryDto>?> GetCachedListAsync(CancellationToken cancellationToken)
    {
        byte[]? cached = await _cache.GetAsync(CacheKey, cancellationToken).ConfigureAwait(false);
        return cached is null ? null : JsonSerializer.Deserialize<List<SupplierCategoryDto>>(cached);
    }

    private async Task SetCacheAsync(IReadOnlyList<SupplierCategoryDto> items, CancellationToken cancellationToken)
    {
        byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(items);
        DistributedCacheEntryOptions options = new() { AbsoluteExpirationRelativeToNow = CacheDuration };
        await _cache.SetAsync(CacheKey, serialized, options, cancellationToken).ConfigureAwait(false);
    }

    private async Task InvalidateCacheAsync(CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(CacheKey, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result?> ValidateUniqueNameAsync(string name, int? excludeId, CancellationToken cancellationToken)
    {
        IQueryable<SupplierCategory> query = Context.SupplierCategories.Where(c => c.Name == name);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_CATEGORY_NAME", "A category with this name already exists.", 409)
            : null;
    }
}
