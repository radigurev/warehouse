using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.GenericFiltering;
using Warehouse.Inventory.API.Interfaces.Products;
using Warehouse.Inventory.API.Services.Base;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Services.Products;

/// <summary>
/// Implements product lifecycle operations: CRUD, search, soft-delete, and reactivation.
/// <para>See <see cref="IProductService"/>.</para>
/// </summary>
public sealed class ProductService : BaseInventoryEntityService, IProductService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public ProductService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task<Result<ProductDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Product? product = await GetProductWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (product is null || product.IsDeleted)
            return Result<ProductDetailDto>.Failure("PRODUCT_NOT_FOUND", "Product not found.", 404);

        ProductDetailDto dto = Mapper.Map<ProductDetailDto>(product);
        return Result<ProductDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<ProductDto>>> SearchAsync(
        SearchProductsRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<Product> query = BuildSearchQuery(request);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        List<Product> items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ProductDto> dtos = Mapper.Map<IReadOnlyList<ProductDto>>(items);

        PaginatedResponse<ProductDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<ProductDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<ProductDetailDto>> CreateAsync(
        CreateProductRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        Result? codeValidation = await ValidateUniqueCodeAsync(request.Code, null, cancellationToken).ConfigureAwait(false);
        if (codeValidation is not null)
            return Result<ProductDetailDto>.Failure(codeValidation.ErrorCode!, codeValidation.ErrorMessage!, codeValidation.StatusCode!.Value);

        Result? categoryValidation = await ValidateCategoryExistsAsync(request.CategoryId, cancellationToken).ConfigureAwait(false);
        if (categoryValidation is not null)
            return Result<ProductDetailDto>.Failure(categoryValidation.ErrorCode!, categoryValidation.ErrorMessage!, categoryValidation.StatusCode!.Value);

        Result? unitValidation = await ValidateUnitExistsAsync(request.UnitOfMeasureId, cancellationToken).ConfigureAwait(false);
        if (unitValidation is not null)
            return Result<ProductDetailDto>.Failure(unitValidation.ErrorCode!, unitValidation.ErrorMessage!, unitValidation.StatusCode!.Value);

        Product product = new()
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            Barcode = request.Barcode,
            CategoryId = request.CategoryId,
            UnitOfMeasureId = request.UnitOfMeasureId,
            Notes = request.Notes,
            RequiresBatchTracking = request.RequiresBatchTracking,
            IsActive = true,
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        Context.Products.Add(product);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Product? created = await GetProductWithDetailsAsync(product.Id, cancellationToken).ConfigureAwait(false);
        ProductDetailDto dto = Mapper.Map<ProductDetailDto>(created!);
        return Result<ProductDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<ProductDetailDto>> UpdateAsync(
        int id,
        UpdateProductRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        Product? product = await GetProductWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (product is null || product.IsDeleted)
            return Result<ProductDetailDto>.Failure("PRODUCT_NOT_FOUND", "Product not found.", 404);

        Result? categoryValidation = await ValidateCategoryExistsAsync(request.CategoryId, cancellationToken).ConfigureAwait(false);
        if (categoryValidation is not null)
            return Result<ProductDetailDto>.Failure(categoryValidation.ErrorCode!, categoryValidation.ErrorMessage!, categoryValidation.StatusCode!.Value);

        Result? unitValidation = await ValidateUnitExistsAsync(request.UnitOfMeasureId, cancellationToken).ConfigureAwait(false);
        if (unitValidation is not null)
            return Result<ProductDetailDto>.Failure(unitValidation.ErrorCode!, unitValidation.ErrorMessage!, unitValidation.StatusCode!.Value);

        product.Name = request.Name;
        product.Description = request.Description;
        product.Sku = request.Sku;
        product.Barcode = request.Barcode;
        product.CategoryId = request.CategoryId;
        product.UnitOfMeasureId = request.UnitOfMeasureId;
        product.Notes = request.Notes;

        if (request.RequiresBatchTracking.HasValue)
            product.RequiresBatchTracking = request.RequiresBatchTracking.Value;

        product.ModifiedAtUtc = DateTime.UtcNow;
        product.ModifiedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Product? updated = await GetProductWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        ProductDetailDto dto = Mapper.Map<ProductDetailDto>(updated!);
        return Result<ProductDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        Product? product = await Context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (product is null || product.IsDeleted)
            return Result.Failure("PRODUCT_NOT_FOUND", "Product not found.", 404);

        product.IsDeleted = true;
        product.DeletedAtUtc = DateTime.UtcNow;
        product.IsActive = false;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<ProductDetailDto>> ReactivateAsync(
        int id,
        int userId,
        CancellationToken cancellationToken)
    {
        Product? product = await GetProductWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (product is null)
            return Result<ProductDetailDto>.Failure("PRODUCT_NOT_FOUND", "Product not found.", 404);

        if (!product.IsDeleted && product.IsActive)
            return Result<ProductDetailDto>.Failure("PRODUCT_ALREADY_ACTIVE", "Product is already active.", 409);

        Result? codeConflict = await ValidateUniqueCodeAsync(product.Code, id, cancellationToken).ConfigureAwait(false);
        if (codeConflict is not null)
            return Result<ProductDetailDto>.Failure(codeConflict.ErrorCode!, codeConflict.ErrorMessage!, codeConflict.StatusCode!.Value);

        product.IsDeleted = false;
        product.DeletedAtUtc = null;
        product.IsActive = true;
        product.ModifiedAtUtc = DateTime.UtcNow;
        product.ModifiedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Product? reactivated = await GetProductWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        ProductDetailDto dto = Mapper.Map<ProductDetailDto>(reactivated!);
        return Result<ProductDetailDto>.Success(dto);
    }

    /// <summary>
    /// Loads a product with category and unit of measure details.
    /// </summary>
    private async Task<Product?> GetProductWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.Products
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the search query with optional filters.
    /// </summary>
    private IQueryable<Product> BuildSearchQuery(SearchProductsRequest request)
    {
        IQueryable<Product> query = Context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure);

        if (!request.IncludeDeleted)
            query = query.Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(p => p.Name.Contains(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Code))
            query = query.Where(p => p.Code.StartsWith(request.Code));

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        query = query.ApplyFilter(request.Filter);

        return query;
    }

    /// <summary>
    /// Applies sorting to the query based on the sort field.
    /// </summary>
    private static IQueryable<Product> ApplySorting(
        IQueryable<Product> query,
        string? sortBy,
        bool sortDescending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "code" => sortDescending ? query.OrderByDescending(p => p.Code) : query.OrderBy(p => p.Code),
            "createdatutc" => sortDescending ? query.OrderByDescending(p => p.CreatedAtUtc) : query.OrderBy(p => p.CreatedAtUtc),
            _ => sortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
        };
    }

    /// <summary>
    /// Validates product code uniqueness across all products.
    /// </summary>
    private async Task<Result?> ValidateUniqueCodeAsync(
        string code,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<Product> query = Context.Products.Where(p => p.Code == code);

        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_PRODUCT_CODE", "A product with this code already exists.", 409)
            : null;
    }

    /// <summary>
    /// Validates that the category ID references an existing category.
    /// </summary>
    private async Task<Result?> ValidateCategoryExistsAsync(
        int? categoryId,
        CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue)
            return null;

        bool exists = await Context.ProductCategories
            .AnyAsync(c => c.Id == categoryId.Value, cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? null
            : Result.Failure("INVALID_CATEGORY", "The specified product category does not exist.", 400);
    }

    /// <summary>
    /// Validates that the unit of measure ID references an existing unit.
    /// </summary>
    private async Task<Result?> ValidateUnitExistsAsync(
        int unitId,
        CancellationToken cancellationToken)
    {
        bool exists = await Context.UnitsOfMeasure
            .AnyAsync(u => u.Id == unitId, cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? null
            : Result.Failure("INVALID_UNIT_OF_MEASURE", "The specified unit of measure does not exist.", 400);
    }
}
