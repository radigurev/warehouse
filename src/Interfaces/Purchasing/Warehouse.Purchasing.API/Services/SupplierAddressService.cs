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
/// Implements CRUD operations for supplier addresses with default-flag management.
/// <para>See <see cref="ISupplierAddressService"/>.</para>
/// </summary>
public sealed class SupplierAddressService : BasePurchasingEntityService, ISupplierAddressService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public SupplierAddressService(PurchasingDbContext context, IMapper mapper) : base(context, mapper) { }

    /// <inheritdoc />
    public async Task<Result<SupplierAddressDto>> CreateAsync(int supplierId, CreateSupplierAddressRequest request, CancellationToken cancellationToken)
    {
        Result? validation = await ValidateSupplierExistsAsync(supplierId, cancellationToken).ConfigureAwait(false);
        if (validation is not null) return Result<SupplierAddressDto>.Failure(validation.ErrorCode!, validation.ErrorMessage!, validation.StatusCode!.Value);

        bool isFirstOfType = !await Context.SupplierAddresses
            .AnyAsync(a => a.SupplierId == supplierId && a.AddressType == request.AddressType, cancellationToken).ConfigureAwait(false);

        SupplierAddress address = new()
        {
            SupplierId = supplierId, AddressType = request.AddressType, StreetLine1 = request.StreetLine1,
            StreetLine2 = request.StreetLine2, City = request.City, StateProvince = request.StateProvince,
            PostalCode = request.PostalCode, CountryCode = request.CountryCode, IsDefault = isFirstOfType, CreatedAtUtc = DateTime.UtcNow
        };

        Context.SupplierAddresses.Add(address);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapToResult<SupplierAddress, SupplierAddressDto>(address);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SupplierAddressDto>>> GetAllAsync(int supplierId, CancellationToken cancellationToken)
    {
        Result? validation = await ValidateSupplierExistsAsync(supplierId, cancellationToken).ConfigureAwait(false);
        if (validation is not null) return Result<IReadOnlyList<SupplierAddressDto>>.Failure(validation.ErrorCode!, validation.ErrorMessage!, validation.StatusCode!.Value);

        List<SupplierAddress> addresses = await Context.SupplierAddresses.AsNoTracking()
            .Where(a => a.SupplierId == supplierId).OrderBy(a => a.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        return MapListToResult<SupplierAddress, SupplierAddressDto>(addresses);
    }

    /// <inheritdoc />
    public async Task<Result<SupplierAddressDto>> UpdateAsync(int supplierId, int addressId, UpdateSupplierAddressRequest request, CancellationToken cancellationToken)
    {
        SupplierAddress? address = await Context.SupplierAddresses.FirstOrDefaultAsync(a => a.Id == addressId && a.SupplierId == supplierId, cancellationToken).ConfigureAwait(false);
        if (address is null) return Result<SupplierAddressDto>.Failure("ADDRESS_NOT_FOUND", "Supplier address not found.", 404);

        address.AddressType = request.AddressType; address.StreetLine1 = request.StreetLine1; address.StreetLine2 = request.StreetLine2;
        address.City = request.City; address.StateProvince = request.StateProvince; address.PostalCode = request.PostalCode;
        address.CountryCode = request.CountryCode; address.ModifiedAtUtc = DateTime.UtcNow;

        if (request.IsDefault && !address.IsDefault)
            await PrimaryFlagHelper.UnsetOthersAsync(Context.SupplierAddresses, a => a.SupplierId == supplierId && a.AddressType == request.AddressType && a.IsDefault, addressId, a => a.IsDefault = false, cancellationToken).ConfigureAwait(false);
        address.IsDefault = request.IsDefault;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapToResult<SupplierAddress, SupplierAddressDto>(address);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int supplierId, int addressId, CancellationToken cancellationToken)
    {
        SupplierAddress? address = await Context.SupplierAddresses.FirstOrDefaultAsync(a => a.Id == addressId && a.SupplierId == supplierId, cancellationToken).ConfigureAwait(false);
        if (address is null) return Result.Failure("ADDRESS_NOT_FOUND", "Supplier address not found.", 404);

        bool wasDefault = address.IsDefault;
        string addressType = address.AddressType;
        Context.SupplierAddresses.Remove(address);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (wasDefault)
        {
            await PrimaryFlagHelper.PromoteNextAsync(Context.SupplierAddresses, a => a.SupplierId == supplierId && a.AddressType == addressType, a => a.CreatedAtUtc, a => a.IsDefault = true, cancellationToken).ConfigureAwait(false);
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
