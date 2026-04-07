using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.GenericFiltering;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.Purchasing.API.Services.Base;
using Warehouse.Purchasing.DBModel;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Services;

/// <summary>
/// Implements supplier lifecycle operations: CRUD, search, soft-delete, and reactivation.
/// <para>See <see cref="ISupplierService"/>.</para>
/// </summary>
public sealed class SupplierService : BasePurchasingEntityService, ISupplierService
{
    private readonly IPurchaseEventService _eventService;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public SupplierService(PurchasingDbContext context, IMapper mapper, IPurchaseEventService eventService)
        : base(context, mapper)
    {
        _eventService = eventService;
    }

    /// <inheritdoc />
    public async Task<Result<SupplierDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Supplier? supplier = await GetSupplierWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (supplier is null || supplier.IsDeleted)
            return Result<SupplierDetailDto>.Failure("SUPPLIER_NOT_FOUND", "Supplier not found.", 404);

        SupplierDetailDto dto = Mapper.Map<SupplierDetailDto>(supplier);
        return Result<SupplierDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<SupplierDto>>> SearchAsync(
        SearchSuppliersRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<Supplier> query = BuildSearchQuery(request);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        List<Supplier> items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<SupplierDto> dtos = Mapper.Map<IReadOnlyList<SupplierDto>>(items);

        PaginatedResponse<SupplierDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<SupplierDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<SupplierDetailDto>> CreateAsync(
        CreateSupplierRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        string code = string.IsNullOrWhiteSpace(request.Code)
            ? await GenerateSupplierCodeAsync(cancellationToken).ConfigureAwait(false)
            : request.Code;

        Result? codeValidation = await ValidateUniqueCodeAsync(code, null, cancellationToken).ConfigureAwait(false);
        if (codeValidation is not null)
            return Result<SupplierDetailDto>.Failure(codeValidation.ErrorCode!, codeValidation.ErrorMessage!, codeValidation.StatusCode!.Value);

        Result? taxIdValidation = await ValidateUniqueTaxIdAsync(request.TaxId, null, cancellationToken).ConfigureAwait(false);
        if (taxIdValidation is not null)
            return Result<SupplierDetailDto>.Failure(taxIdValidation.ErrorCode!, taxIdValidation.ErrorMessage!, taxIdValidation.StatusCode!.Value);

        Result? categoryValidation = await ValidateCategoryExistsAsync(request.CategoryId, cancellationToken).ConfigureAwait(false);
        if (categoryValidation is not null)
            return Result<SupplierDetailDto>.Failure(categoryValidation.ErrorCode!, categoryValidation.ErrorMessage!, categoryValidation.StatusCode!.Value);

        Supplier supplier = new()
        {
            Code = code,
            Name = request.Name,
            TaxId = request.TaxId,
            CategoryId = request.CategoryId,
            PaymentTermDays = request.PaymentTermDays,
            Notes = request.Notes,
            IsActive = true,
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        Context.Suppliers.Add(supplier);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _eventService.RecordEventAsync("SupplierCreated", "Supplier", supplier.Id, userId, null, cancellationToken).ConfigureAwait(false);

        Supplier? created = await GetSupplierWithDetailsAsync(supplier.Id, cancellationToken).ConfigureAwait(false);
        SupplierDetailDto dto = Mapper.Map<SupplierDetailDto>(created!);
        return Result<SupplierDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<SupplierDetailDto>> UpdateAsync(
        int id,
        UpdateSupplierRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        Supplier? supplier = await GetSupplierWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (supplier is null || supplier.IsDeleted)
            return Result<SupplierDetailDto>.Failure("SUPPLIER_NOT_FOUND", "Supplier not found.", 404);

        Result? taxIdValidation = await ValidateUniqueTaxIdAsync(request.TaxId, id, cancellationToken).ConfigureAwait(false);
        if (taxIdValidation is not null)
            return Result<SupplierDetailDto>.Failure(taxIdValidation.ErrorCode!, taxIdValidation.ErrorMessage!, taxIdValidation.StatusCode!.Value);

        Result? categoryValidation = await ValidateCategoryExistsAsync(request.CategoryId, cancellationToken).ConfigureAwait(false);
        if (categoryValidation is not null)
            return Result<SupplierDetailDto>.Failure(categoryValidation.ErrorCode!, categoryValidation.ErrorMessage!, categoryValidation.StatusCode!.Value);

        supplier.Name = request.Name;
        supplier.TaxId = request.TaxId;
        supplier.CategoryId = request.CategoryId;
        supplier.PaymentTermDays = request.PaymentTermDays;
        supplier.Notes = request.Notes;
        supplier.ModifiedAtUtc = DateTime.UtcNow;
        supplier.ModifiedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _eventService.RecordEventAsync("SupplierUpdated", "Supplier", id, userId, null, cancellationToken).ConfigureAwait(false);

        Supplier? updated = await GetSupplierWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        SupplierDetailDto dto = Mapper.Map<SupplierDetailDto>(updated!);
        return Result<SupplierDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        Supplier? supplier = await Context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null || supplier.IsDeleted)
            return Result.Failure("SUPPLIER_NOT_FOUND", "Supplier not found.", 404);

        int openPoCount = await Context.PurchaseOrders
            .CountAsync(po => po.SupplierId == id &&
                (po.Status == nameof(PurchaseOrderStatus.Draft) || po.Status == nameof(PurchaseOrderStatus.Confirmed)),
                cancellationToken)
            .ConfigureAwait(false);

        if (openPoCount > 0)
            return Result.Failure("SUPPLIER_HAS_OPEN_POS", $"Cannot deactivate supplier -- {openPoCount} purchase order(s) are still open.", 409);

        supplier.IsDeleted = true;
        supplier.DeletedAtUtc = DateTime.UtcNow;
        supplier.IsActive = false;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<SupplierDetailDto>> ReactivateAsync(
        int id,
        int userId,
        CancellationToken cancellationToken)
    {
        Supplier? supplier = await GetSupplierWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (supplier is null)
            return Result<SupplierDetailDto>.Failure("SUPPLIER_NOT_FOUND", "Supplier not found.", 404);

        if (!supplier.IsDeleted && supplier.IsActive)
            return Result<SupplierDetailDto>.Failure("SUPPLIER_ALREADY_ACTIVE", "Supplier is already active.", 409);

        Result? codeConflict = await ValidateReactivationCodeAsync(supplier.Code, id, cancellationToken).ConfigureAwait(false);
        if (codeConflict is not null)
            return Result<SupplierDetailDto>.Failure(codeConflict.ErrorCode!, codeConflict.ErrorMessage!, codeConflict.StatusCode!.Value);

        Result? taxIdConflict = await ValidateReactivationTaxIdAsync(supplier.TaxId, id, cancellationToken).ConfigureAwait(false);
        if (taxIdConflict is not null)
            return Result<SupplierDetailDto>.Failure(taxIdConflict.ErrorCode!, taxIdConflict.ErrorMessage!, taxIdConflict.StatusCode!.Value);

        supplier.IsDeleted = false;
        supplier.DeletedAtUtc = null;
        supplier.IsActive = true;
        supplier.ModifiedAtUtc = DateTime.UtcNow;
        supplier.ModifiedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _eventService.RecordEventAsync("SupplierReactivated", "Supplier", id, userId, null, cancellationToken).ConfigureAwait(false);

        Supplier? reactivated = await GetSupplierWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        SupplierDetailDto dto = Mapper.Map<SupplierDetailDto>(reactivated!);
        return Result<SupplierDetailDto>.Success(dto);
    }

    private async Task<Supplier?> GetSupplierWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.Suppliers
            .Include(s => s.Category)
            .Include(s => s.Addresses)
            .Include(s => s.Phones)
            .Include(s => s.Emails)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    private IQueryable<Supplier> BuildSearchQuery(SearchSuppliersRequest request)
    {
        IQueryable<Supplier> query = Context.Suppliers
            .AsNoTracking()
            .Include(s => s.Category);

        if (!request.IncludeDeleted)
            query = query.Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(s => s.Name.Contains(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Code))
            query = query.Where(s => s.Code.StartsWith(request.Code));

        if (!string.IsNullOrWhiteSpace(request.TaxId))
            query = query.Where(s => s.TaxId == request.TaxId);

        if (request.CategoryId.HasValue)
            query = query.Where(s => s.CategoryId == request.CategoryId.Value);

        query = query.ApplyFilter(request.Filter);

        return query;
    }

    private static IQueryable<Supplier> ApplySorting(IQueryable<Supplier> query, string? sortBy, bool sortDescending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "code" => sortDescending ? query.OrderByDescending(s => s.Code) : query.OrderBy(s => s.Code),
            "createdatutc" => sortDescending ? query.OrderByDescending(s => s.CreatedAtUtc) : query.OrderBy(s => s.CreatedAtUtc),
            _ => sortDescending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name)
        };
    }

    private async Task<string> GenerateSupplierCodeAsync(CancellationToken cancellationToken)
    {
        int maxNumber = await Context.Suppliers
            .Where(s => s.Code.StartsWith("SUPP-"))
            .Select(s => s.Code.Substring(5))
            .Select(s => Convert.ToInt32(s))
            .DefaultIfEmpty(0)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);

        return $"SUPP-{(maxNumber + 1):D6}";
    }

    private async Task<Result?> ValidateUniqueCodeAsync(string code, int? excludeId, CancellationToken cancellationToken)
    {
        IQueryable<Supplier> query = Context.Suppliers.Where(s => s.Code == code);
        if (excludeId.HasValue) query = query.Where(s => s.Id != excludeId.Value);
        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);
        return exists ? Result.Failure("DUPLICATE_SUPPLIER_CODE", "A supplier with this code already exists.", 409) : null;
    }

    private async Task<Result?> ValidateUniqueTaxIdAsync(string? taxId, int? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(taxId)) return null;
        IQueryable<Supplier> query = Context.Suppliers.Where(s => s.TaxId == taxId && !s.IsDeleted);
        if (excludeId.HasValue) query = query.Where(s => s.Id != excludeId.Value);
        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);
        return exists ? Result.Failure("DUPLICATE_TAX_ID", "An active supplier with this tax ID already exists.", 409) : null;
    }

    private async Task<Result?> ValidateCategoryExistsAsync(int? categoryId, CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue) return null;
        bool exists = await Context.SupplierCategories.AnyAsync(c => c.Id == categoryId.Value, cancellationToken).ConfigureAwait(false);
        return exists ? null : Result.Failure("INVALID_CATEGORY", "The specified supplier category does not exist.", 400);
    }

    private async Task<Result?> ValidateReactivationCodeAsync(string code, int excludeId, CancellationToken cancellationToken)
    {
        bool conflict = await Context.Suppliers.AnyAsync(s => s.Code == code && s.Id != excludeId && !s.IsDeleted, cancellationToken).ConfigureAwait(false);
        return conflict ? Result.Failure("DUPLICATE_SUPPLIER_CODE", "A supplier with this code already exists.", 409) : null;
    }

    private async Task<Result?> ValidateReactivationTaxIdAsync(string? taxId, int excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(taxId)) return null;
        bool conflict = await Context.Suppliers.AnyAsync(s => s.TaxId == taxId && s.Id != excludeId && !s.IsDeleted, cancellationToken).ConfigureAwait(false);
        return conflict ? Result.Failure("DUPLICATE_TAX_ID", "An active supplier with this tax ID already exists.", 409) : null;
    }
}
