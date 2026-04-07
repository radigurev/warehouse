using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Services;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.Purchasing.API.Services.Base;
using Warehouse.Purchasing.DBModel;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Services;

/// <summary>
/// Implements CRUD operations for supplier phones with primary-flag management.
/// <para>See <see cref="ISupplierPhoneService"/>.</para>
/// </summary>
public sealed class SupplierPhoneService : BasePurchasingEntityService, ISupplierPhoneService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public SupplierPhoneService(PurchasingDbContext context, IMapper mapper) : base(context, mapper) { }

    /// <inheritdoc />
    public async Task<Result<SupplierPhoneDto>> CreateAsync(int supplierId, CreateSupplierPhoneRequest request, CancellationToken cancellationToken)
    {
        Result? validation = await ValidateSupplierExistsAsync(supplierId, cancellationToken).ConfigureAwait(false);
        if (validation is not null) return Result<SupplierPhoneDto>.Failure(validation.ErrorCode!, validation.ErrorMessage!, validation.StatusCode!.Value);

        bool isFirst = !await Context.SupplierPhones.AnyAsync(p => p.SupplierId == supplierId, cancellationToken).ConfigureAwait(false);

        SupplierPhone phone = new() { SupplierId = supplierId, PhoneType = request.PhoneType, PhoneNumber = request.PhoneNumber, Extension = request.Extension, IsPrimary = isFirst, CreatedAtUtc = DateTime.UtcNow };
        Context.SupplierPhones.Add(phone);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapToResult<SupplierPhone, SupplierPhoneDto>(phone);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SupplierPhoneDto>>> GetAllAsync(int supplierId, CancellationToken cancellationToken)
    {
        Result? validation = await ValidateSupplierExistsAsync(supplierId, cancellationToken).ConfigureAwait(false);
        if (validation is not null) return Result<IReadOnlyList<SupplierPhoneDto>>.Failure(validation.ErrorCode!, validation.ErrorMessage!, validation.StatusCode!.Value);

        List<SupplierPhone> phones = await Context.SupplierPhones.AsNoTracking().Where(p => p.SupplierId == supplierId).OrderBy(p => p.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        return MapListToResult<SupplierPhone, SupplierPhoneDto>(phones);
    }

    /// <inheritdoc />
    public async Task<Result<SupplierPhoneDto>> UpdateAsync(int supplierId, int phoneId, UpdateSupplierPhoneRequest request, CancellationToken cancellationToken)
    {
        SupplierPhone? phone = await Context.SupplierPhones.FirstOrDefaultAsync(p => p.Id == phoneId && p.SupplierId == supplierId, cancellationToken).ConfigureAwait(false);
        if (phone is null) return Result<SupplierPhoneDto>.Failure("PHONE_NOT_FOUND", "Supplier phone not found.", 404);

        phone.PhoneType = request.PhoneType; phone.PhoneNumber = request.PhoneNumber; phone.Extension = request.Extension; phone.ModifiedAtUtc = DateTime.UtcNow;

        if (request.IsPrimary && !phone.IsPrimary)
            await PrimaryFlagHelper.UnsetOthersAsync(Context.SupplierPhones, p => p.SupplierId == supplierId && p.IsPrimary, phoneId, p => p.IsPrimary = false, cancellationToken).ConfigureAwait(false);
        phone.IsPrimary = request.IsPrimary;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapToResult<SupplierPhone, SupplierPhoneDto>(phone);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int supplierId, int phoneId, CancellationToken cancellationToken)
    {
        SupplierPhone? phone = await Context.SupplierPhones.FirstOrDefaultAsync(p => p.Id == phoneId && p.SupplierId == supplierId, cancellationToken).ConfigureAwait(false);
        if (phone is null) return Result.Failure("PHONE_NOT_FOUND", "Supplier phone not found.", 404);

        bool wasPrimary = phone.IsPrimary;
        Context.SupplierPhones.Remove(phone);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (wasPrimary)
        {
            await PrimaryFlagHelper.PromoteNextAsync(Context.SupplierPhones, p => p.SupplierId == supplierId, p => p.CreatedAtUtc, p => p.IsPrimary = true, cancellationToken).ConfigureAwait(false);
            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        return Result.Success();
    }

    private async Task<Result?> ValidateSupplierExistsAsync(int supplierId, CancellationToken cancellationToken)
    {
        bool exists = await Context.Suppliers.AnyAsync(s => s.Id == supplierId && !s.IsDeleted, cancellationToken).ConfigureAwait(false);
        return exists ? null : Result.Failure("SUPPLIER_NOT_FOUND", "Supplier not found.", 404);
    }
}
