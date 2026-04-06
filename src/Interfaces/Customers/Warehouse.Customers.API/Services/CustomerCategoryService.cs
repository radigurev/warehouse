using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Customers.API.Interfaces;
using Warehouse.Customers.DBModel;
using Warehouse.Customers.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Customers.API.Services;

/// <summary>
/// Implements customer category operations: CRUD with uniqueness and in-use checks.
/// <para>See <see cref="ICustomerCategoryService"/>.</para>
/// </summary>
public sealed class CustomerCategoryService : BaseCustomerEntityService, ICustomerCategoryService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public CustomerCategoryService(CustomersDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task<Result<CustomerCategoryDto>> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Result? nameValidation = await ValidateUniqueNameAsync(request.Name, null, cancellationToken).ConfigureAwait(false);
        if (nameValidation is not null)
            return Result<CustomerCategoryDto>.Failure(nameValidation.ErrorCode!, nameValidation.ErrorMessage!, nameValidation.StatusCode!.Value);

        CustomerCategory category = new()
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.CustomerCategories.Add(category);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        CustomerCategoryDto dto = Mapper.Map<CustomerCategoryDto>(category);
        return Result<CustomerCategoryDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<CustomerCategoryDto>>> GetAllAsync(
        PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        IQueryable<CustomerCategory> query = Context.CustomerCategories
            .AsNoTracking()
            .OrderBy(c => c.Name);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<CustomerCategory> categories = await query
            .Skip(pagination.Skip)
            .Take(pagination.EffectivePageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CustomerCategoryDto> dtos = Mapper.Map<IReadOnlyList<CustomerCategoryDto>>(categories);

        PaginatedResponse<CustomerCategoryDto> response = new()
        {
            Items = dtos,
            Page = pagination.Page,
            PageSize = pagination.EffectivePageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<CustomerCategoryDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerCategoryDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        CustomerCategory? category = await Context.CustomerCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
            return Result<CustomerCategoryDto>.Failure("CATEGORY_NOT_FOUND", "Customer category not found.", 404);

        CustomerCategoryDto dto = Mapper.Map<CustomerCategoryDto>(category);
        return Result<CustomerCategoryDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerCategoryDto>> UpdateAsync(
        int id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        CustomerCategory? category = await Context.CustomerCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
            return Result<CustomerCategoryDto>.Failure("CATEGORY_NOT_FOUND", "Customer category not found.", 404);

        Result? nameValidation = await ValidateUniqueNameAsync(request.Name, id, cancellationToken).ConfigureAwait(false);
        if (nameValidation is not null)
            return Result<CustomerCategoryDto>.Failure(nameValidation.ErrorCode!, nameValidation.ErrorMessage!, nameValidation.StatusCode!.Value);

        category.Name = request.Name;
        category.Description = request.Description;
        category.ModifiedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        CustomerCategoryDto dto = Mapper.Map<CustomerCategoryDto>(category);
        return Result<CustomerCategoryDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        CustomerCategory? category = await Context.CustomerCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
            return Result.Failure("CATEGORY_NOT_FOUND", "Customer category not found.", 404);

        int customerCount = await Context.Customers
            .CountAsync(c => c.CategoryId == id, cancellationToken)
            .ConfigureAwait(false);

        if (customerCount > 0)
            return Result.Failure("CATEGORY_IN_USE", $"Cannot delete category — it is assigned to {customerCount} customer(s).", 409);

        Context.CustomerCategories.Remove(category);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    /// <summary>
    /// Validates that the category name is unique across all categories.
    /// </summary>
    private async Task<Result?> ValidateUniqueNameAsync(
        string name,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<CustomerCategory> query = Context.CustomerCategories
            .Where(c => c.Name == name);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_CATEGORY_NAME", "A category with this name already exists.", 409)
            : null;
    }
}
