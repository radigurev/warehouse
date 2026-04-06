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
/// Implements product category management operations.
/// <para>See <see cref="IProductCategoryService"/>.</para>
/// </summary>
public sealed class ProductCategoryService : BaseInventoryEntityService, IProductCategoryService
{
    private const string CacheKey = "inventory:product-categories:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public ProductCategoryService(InventoryDbContext context, IMapper mapper, IDistributedCache cache)
        : base(context, mapper)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<ProductCategoryDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        ProductCategory? category = await Context.ProductCategories
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
            return Result<ProductCategoryDto>.Failure("CATEGORY_NOT_FOUND", "Product category not found.", 404);

        ProductCategoryDto dto = Mapper.Map<ProductCategoryDto>(category);
        return Result<ProductCategoryDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<ProductCategoryDto>>> ListAsync(
        PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductCategoryDto>? cached = await GetCachedListAsync(cancellationToken).ConfigureAwait(false);

        if (cached is not null)
            return PaginateFromList(cached, pagination);

        List<ProductCategory> categories = await Context.ProductCategories
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ProductCategoryDto> allDtos = Mapper.Map<IReadOnlyList<ProductCategoryDto>>(categories);
        await SetCacheAsync(allDtos, cancellationToken).ConfigureAwait(false);

        return PaginateFromList(allDtos, pagination);
    }

    /// <inheritdoc />
    public async Task<Result<ProductCategoryDto>> CreateAsync(
        CreateProductCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Result? nameValidation = await ValidateUniqueNameAsync(request.Name, null, cancellationToken).ConfigureAwait(false);
        if (nameValidation is not null)
            return Result<ProductCategoryDto>.Failure(nameValidation.ErrorCode!, nameValidation.ErrorMessage!, nameValidation.StatusCode!.Value);

        ProductCategory category = new()
        {
            Name = request.Name,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.ProductCategories.Add(category);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);

        ProductCategory? created = await Context.ProductCategories
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == category.Id, cancellationToken)
            .ConfigureAwait(false);

        ProductCategoryDto dto = Mapper.Map<ProductCategoryDto>(created!);
        return Result<ProductCategoryDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<ProductCategoryDto>> UpdateAsync(
        int id,
        UpdateProductCategoryRequest request,
        CancellationToken cancellationToken)
    {
        ProductCategory? category = await Context.ProductCategories
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
            return Result<ProductCategoryDto>.Failure("CATEGORY_NOT_FOUND", "Product category not found.", 404);

        if (request.ParentCategoryId == id)
            return Result<ProductCategoryDto>.Failure("CATEGORY_SELF_PARENT", "A category cannot be its own parent.", 400);

        Result? nameValidation = await ValidateUniqueNameAsync(request.Name, id, cancellationToken).ConfigureAwait(false);
        if (nameValidation is not null)
            return Result<ProductCategoryDto>.Failure(nameValidation.ErrorCode!, nameValidation.ErrorMessage!, nameValidation.StatusCode!.Value);

        category.Name = request.Name;
        category.Description = request.Description;
        category.ParentCategoryId = request.ParentCategoryId;
        category.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);

        ProductCategoryDto dto = Mapper.Map<ProductCategoryDto>(category);
        return Result<ProductCategoryDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        ProductCategory? category = await Context.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
            return Result.Failure("CATEGORY_NOT_FOUND", "Product category not found.", 404);

        int productCount = await Context.Products
            .CountAsync(p => p.CategoryId == id && !p.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (productCount > 0)
            return Result.Failure("CATEGORY_IN_USE", $"Cannot delete category -- it is assigned to {productCount} product(s).", 409);

        int childCount = await Context.ProductCategories
            .CountAsync(c => c.ParentCategoryId == id, cancellationToken)
            .ConfigureAwait(false);

        if (childCount > 0)
            return Result.Failure("CATEGORY_HAS_CHILDREN", $"Cannot delete category -- it has {childCount} child category(ies).", 409);

        Context.ProductCategories.Remove(category);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// Paginates an in-memory list of DTOs.
    /// </summary>
    private static Result<PaginatedResponse<ProductCategoryDto>> PaginateFromList(
        IReadOnlyList<ProductCategoryDto> items,
        PaginationParams pagination)
    {
        PaginatedResponse<ProductCategoryDto> response = new()
        {
            Items = items.Skip(pagination.Skip).Take(pagination.EffectivePageSize).ToList(),
            Page = pagination.Page,
            PageSize = pagination.EffectivePageSize,
            TotalCount = items.Count
        };

        return Result<PaginatedResponse<ProductCategoryDto>>.Success(response);
    }

    /// <summary>
    /// Attempts to read the full category list from cache.
    /// </summary>
    private async Task<IReadOnlyList<ProductCategoryDto>?> GetCachedListAsync(CancellationToken cancellationToken)
    {
        byte[]? cached = await _cache.GetAsync(CacheKey, cancellationToken).ConfigureAwait(false);
        return cached is null ? null : JsonSerializer.Deserialize<List<ProductCategoryDto>>(cached);
    }

    /// <summary>
    /// Stores the full category list in cache.
    /// </summary>
    private async Task SetCacheAsync(IReadOnlyList<ProductCategoryDto> items, CancellationToken cancellationToken)
    {
        byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(items);
        DistributedCacheEntryOptions options = new() { AbsoluteExpirationRelativeToNow = CacheDuration };
        await _cache.SetAsync(CacheKey, serialized, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the category list from cache.
    /// </summary>
    private async Task InvalidateCacheAsync(CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(CacheKey, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates category name uniqueness.
    /// </summary>
    private async Task<Result?> ValidateUniqueNameAsync(
        string name,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<ProductCategory> query = Context.ProductCategories.Where(c => c.Name == name);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_CATEGORY_NAME", "A category with this name already exists.", 409)
            : null;
    }
}
