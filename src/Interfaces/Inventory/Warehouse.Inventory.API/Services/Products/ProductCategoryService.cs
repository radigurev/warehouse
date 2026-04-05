using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Interfaces.Products;
using Warehouse.Inventory.API.Services.Base;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Services.Products;

/// <summary>
/// Implements product category management operations.
/// <para>See <see cref="IProductCategoryService"/>.</para>
/// </summary>
public sealed class ProductCategoryService : BaseInventoryEntityService, IProductCategoryService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public ProductCategoryService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
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
    public async Task<Result<IReadOnlyList<ProductCategoryDto>>> ListAsync(CancellationToken cancellationToken)
    {
        List<ProductCategory> categories = await Context.ProductCategories
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ProductCategoryDto> dtos = Mapper.Map<IReadOnlyList<ProductCategoryDto>>(categories);
        return Result<IReadOnlyList<ProductCategoryDto>>.Success(dtos);
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
        return Result.Success();
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
