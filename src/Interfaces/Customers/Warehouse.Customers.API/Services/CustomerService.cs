using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Customers.API.Interfaces;
using Warehouse.Customers.DBModel;
using Warehouse.Customers.DBModel.Models;
using Warehouse.GenericFiltering;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Customers;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Customers.API.Services;

/// <summary>
/// Implements customer lifecycle operations: CRUD, search, soft-delete, and reactivation.
/// <para>See <see cref="ICustomerService"/>.</para>
/// </summary>
public sealed class CustomerService : BaseCustomerEntityService, ICustomerService
{
    private readonly IPublishEndpoint _publishEndpoint;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public CustomerService(CustomersDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint)
        : base(context, mapper)
    {
        _publishEndpoint = publishEndpoint;
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetNextCodeAsync(CancellationToken cancellationToken)
    {
        string code = await GenerateCustomerCodeAsync(cancellationToken).ConfigureAwait(false);
        return Result<string>.Success(code);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Customer? customer = await GetCustomerWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (customer is null || customer.IsDeleted)
            return Result<CustomerDetailDto>.Failure("CUSTOMER_NOT_FOUND", "Customer not found.", 404);

        CustomerDetailDto dto = Mapper.Map<CustomerDetailDto>(customer);
        return Result<CustomerDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<CustomerDto>>> SearchAsync(
        SearchCustomersRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<Customer> query = BuildSearchQuery(request);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        List<Customer> items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CustomerDto> dtos = Mapper.Map<IReadOnlyList<CustomerDto>>(items);

        PaginatedResponse<CustomerDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<CustomerDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerDetailDto>> CreateAsync(
        CreateCustomerRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        string code = string.IsNullOrWhiteSpace(request.Code)
            ? await GenerateCustomerCodeAsync(cancellationToken).ConfigureAwait(false)
            : request.Code;

        Result? codeValidation = await ValidateUniqueCodeAsync(code, null, cancellationToken).ConfigureAwait(false);
        if (codeValidation is not null)
            return Result<CustomerDetailDto>.Failure(codeValidation.ErrorCode!, codeValidation.ErrorMessage!, codeValidation.StatusCode!.Value);

        Result? taxIdValidation = await ValidateUniqueTaxIdAsync(request.TaxId, null, cancellationToken).ConfigureAwait(false);
        if (taxIdValidation is not null)
            return Result<CustomerDetailDto>.Failure(taxIdValidation.ErrorCode!, taxIdValidation.ErrorMessage!, taxIdValidation.StatusCode!.Value);

        Result? categoryValidation = await ValidateCategoryExistsAsync(request.CategoryId, cancellationToken).ConfigureAwait(false);
        if (categoryValidation is not null)
            return Result<CustomerDetailDto>.Failure(categoryValidation.ErrorCode!, categoryValidation.ErrorMessage!, categoryValidation.StatusCode!.Value);

        Customer customer = new()
        {
            Code = code,
            Name = request.Name,
            NativeLanguageName = request.NativeLanguageName,
            TaxId = request.TaxId,
            CategoryId = request.CategoryId,
            Notes = request.Notes,
            IsActive = true,
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        Context.Customers.Add(customer);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _publishEndpoint.Publish(new CustomerCreatedEvent
            {
                CustomerId = customer.Id,
                Code = customer.Code,
                Name = customer.Name,
                CategoryId = customer.CategoryId,
                CreatedByUserId = userId,
                CreatedAt = customer.CreatedAtUtc
            }, cancellationToken).ConfigureAwait(false);

            await _publishEndpoint.Publish(new CustomerEventOccurredEvent
            {
                EventType = "CustomerCreated",
                EntityType = "Customer",
                EntityId = customer.Id,
                UserId = userId,
                OccurredAtUtc = customer.CreatedAtUtc,
                CustomerName = customer.Name,
                CustomerCode = customer.Code
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
        }

        Customer? created = await GetCustomerWithDetailsAsync(customer.Id, cancellationToken).ConfigureAwait(false);
        CustomerDetailDto dto = Mapper.Map<CustomerDetailDto>(created!);
        return Result<CustomerDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerDetailDto>> UpdateAsync(
        int id,
        UpdateCustomerRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        Customer? customer = await GetCustomerWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (customer is null || customer.IsDeleted)
            return Result<CustomerDetailDto>.Failure("CUSTOMER_NOT_FOUND", "Customer not found.", 404);

        Result? taxIdValidation = await ValidateUniqueTaxIdAsync(request.TaxId, id, cancellationToken).ConfigureAwait(false);
        if (taxIdValidation is not null)
            return Result<CustomerDetailDto>.Failure(taxIdValidation.ErrorCode!, taxIdValidation.ErrorMessage!, taxIdValidation.StatusCode!.Value);

        Result? categoryValidation = await ValidateCategoryExistsAsync(request.CategoryId, cancellationToken).ConfigureAwait(false);
        if (categoryValidation is not null)
            return Result<CustomerDetailDto>.Failure(categoryValidation.ErrorCode!, categoryValidation.ErrorMessage!, categoryValidation.StatusCode!.Value);

        customer.Name = request.Name;
        customer.NativeLanguageName = request.NativeLanguageName;
        customer.TaxId = request.TaxId;
        customer.CategoryId = request.CategoryId;
        customer.Notes = request.Notes;
        customer.ModifiedAtUtc = DateTime.UtcNow;
        customer.ModifiedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Customer? updated = await GetCustomerWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        CustomerDetailDto dto = Mapper.Map<CustomerDetailDto>(updated!);
        return Result<CustomerDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        Customer? customer = await Context.Customers
            .Include(c => c.Accounts.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (customer is null || customer.IsDeleted)
            return Result.Failure("CUSTOMER_NOT_FOUND", "Customer not found.", 404);

        customer.IsDeleted = true;
        customer.DeletedAtUtc = DateTime.UtcNow;
        customer.IsActive = false;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<CustomerDetailDto>> ReactivateAsync(
        int id,
        int userId,
        CancellationToken cancellationToken)
    {
        Customer? customer = await GetCustomerWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (customer is null)
            return Result<CustomerDetailDto>.Failure("CUSTOMER_NOT_FOUND", "Customer not found.", 404);

        if (!customer.IsDeleted && customer.IsActive)
            return Result<CustomerDetailDto>.Failure("CUSTOMER_ALREADY_ACTIVE", "Customer is already active.", 409);

        Result? codeConflict = await ValidateReactivationCodeAsync(customer.Code, id, cancellationToken).ConfigureAwait(false);
        if (codeConflict is not null)
            return Result<CustomerDetailDto>.Failure(codeConflict.ErrorCode!, codeConflict.ErrorMessage!, codeConflict.StatusCode!.Value);

        Result? taxIdConflict = await ValidateReactivationTaxIdAsync(customer.TaxId, id, cancellationToken).ConfigureAwait(false);
        if (taxIdConflict is not null)
            return Result<CustomerDetailDto>.Failure(taxIdConflict.ErrorCode!, taxIdConflict.ErrorMessage!, taxIdConflict.StatusCode!.Value);

        customer.IsDeleted = false;
        customer.DeletedAtUtc = null;
        customer.IsActive = true;
        customer.ModifiedAtUtc = DateTime.UtcNow;
        customer.ModifiedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Customer? reactivated = await GetCustomerWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        CustomerDetailDto dto = Mapper.Map<CustomerDetailDto>(reactivated!);
        return Result<CustomerDetailDto>.Success(dto);
    }

    /// <summary>
    /// Loads a customer with all nested details for mapping.
    /// </summary>
    private async Task<Customer?> GetCustomerWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.Customers
            .Include(c => c.Category)
            .Include(c => c.Accounts.Where(a => !a.IsDeleted))
            .Include(c => c.Addresses)
            .Include(c => c.Phones)
            .Include(c => c.Emails)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the search query with optional filters.
    /// </summary>
    private IQueryable<Customer> BuildSearchQuery(SearchCustomersRequest request)
    {
        IQueryable<Customer> query = Context.Customers
            .AsNoTracking()
            .Include(c => c.Category);

        if (!request.IncludeDeleted)
            query = query.Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(c => c.Name.Contains(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Code))
            query = query.Where(c => c.Code.StartsWith(request.Code));

        if (!string.IsNullOrWhiteSpace(request.TaxId))
            query = query.Where(c => c.TaxId == request.TaxId);

        if (request.CategoryId.HasValue)
            query = query.Where(c => c.CategoryId == request.CategoryId.Value);

        query = query.ApplyFilter(request.Filter);

        return query;
    }

    /// <summary>
    /// Applies sorting to the query based on the sort field.
    /// </summary>
    private static IQueryable<Customer> ApplySorting(
        IQueryable<Customer> query,
        string? sortBy,
        bool sortDescending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "code" => sortDescending ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code),
            "createdatutc" => sortDescending ? query.OrderByDescending(c => c.CreatedAtUtc) : query.OrderBy(c => c.CreatedAtUtc),
            _ => sortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name)
        };
    }

    /// <summary>
    /// Generates the next sequential customer code in CUST-NNNNNN format.
    /// </summary>
    private async Task<string> GenerateCustomerCodeAsync(CancellationToken cancellationToken)
    {
        List<string> codes = await Context.Customers
            .Where(c => c.Code.StartsWith("CUST-"))
            .Select(c => c.Code.Substring(5))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        int maxNumber = codes
            .Select(c => int.TryParse(c, out int n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"CUST-{(maxNumber + 1):D6}";
    }

    /// <summary>
    /// Validates customer code uniqueness across all customers.
    /// </summary>
    private async Task<Result?> ValidateUniqueCodeAsync(
        string code,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<Customer> query = Context.Customers.Where(c => c.Code == code);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_CUSTOMER_CODE", "A customer with this code already exists.", 409)
            : null;
    }

    /// <summary>
    /// Validates tax ID uniqueness among active customers.
    /// </summary>
    private async Task<Result?> ValidateUniqueTaxIdAsync(
        string? taxId,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(taxId))
            return null;

        IQueryable<Customer> query = Context.Customers
            .Where(c => c.TaxId == taxId && !c.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_TAX_ID", "An active customer with this tax ID already exists.", 409)
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

        bool exists = await Context.CustomerCategories
            .AnyAsync(c => c.Id == categoryId.Value, cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? null
            : Result.Failure("INVALID_CATEGORY", "The specified customer category does not exist.", 400);
    }

    /// <summary>
    /// Validates that reactivation will not conflict with an existing active customer code.
    /// </summary>
    private async Task<Result?> ValidateReactivationCodeAsync(
        string code,
        int excludeId,
        CancellationToken cancellationToken)
    {
        bool conflict = await Context.Customers
            .AnyAsync(c => c.Code == code && c.Id != excludeId && !c.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return conflict
            ? Result.Failure("DUPLICATE_CUSTOMER_CODE", "A customer with this code already exists.", 409)
            : null;
    }

    /// <summary>
    /// Validates that reactivation will not conflict with an existing active customer tax ID.
    /// </summary>
    private async Task<Result?> ValidateReactivationTaxIdAsync(
        string? taxId,
        int excludeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(taxId))
            return null;

        bool conflict = await Context.Customers
            .AnyAsync(c => c.TaxId == taxId && c.Id != excludeId && !c.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return conflict
            ? Result.Failure("DUPLICATE_TAX_ID", "An active customer with this tax ID already exists.", 409)
            : null;
    }
}
