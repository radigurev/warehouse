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

/// <summary>
/// Implements CRUD operations for supplier emails with primary-flag and uniqueness management.
/// <para>See <see cref="ISupplierEmailService"/>.</para>
/// </summary>
public sealed class SupplierEmailService : BasePurchasingEntityService, ISupplierEmailService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public SupplierEmailService(PurchasingDbContext context, IMapper mapper) : base(context, mapper) { }

    /// <inheritdoc />
    public async Task<Result<SupplierEmailDto>> CreateAsync(int supplierId, CreateSupplierEmailRequest request, CancellationToken cancellationToken)
    {
        Result? validation = await ValidateSupplierExistsAsync(supplierId, cancellationToken).ConfigureAwait(false);
        if (validation is not null) return Result<SupplierEmailDto>.Failure(validation.ErrorCode!, validation.ErrorMessage!, validation.StatusCode!.Value);

        bool duplicate = await Context.SupplierEmails.AnyAsync(e => e.SupplierId == supplierId && e.EmailAddress == request.EmailAddress, cancellationToken).ConfigureAwait(false);
        if (duplicate) return Result<SupplierEmailDto>.Failure("DUPLICATE_SUPPLIER_EMAIL", "This supplier already has this email address.", 409);

        bool isFirst = !await Context.SupplierEmails.AnyAsync(e => e.SupplierId == supplierId, cancellationToken).ConfigureAwait(false);

        SupplierEmail email = new() { SupplierId = supplierId, EmailType = request.EmailType, EmailAddress = request.EmailAddress, IsPrimary = isFirst, CreatedAtUtc = DateTime.UtcNow };
        Context.SupplierEmails.Add(email);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapToResult<SupplierEmail, SupplierEmailDto>(email);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SupplierEmailDto>>> GetAllAsync(int supplierId, CancellationToken cancellationToken)
    {
        Result? validation = await ValidateSupplierExistsAsync(supplierId, cancellationToken).ConfigureAwait(false);
        if (validation is not null) return Result<IReadOnlyList<SupplierEmailDto>>.Failure(validation.ErrorCode!, validation.ErrorMessage!, validation.StatusCode!.Value);

        List<SupplierEmail> emails = await Context.SupplierEmails.AsNoTracking().Where(e => e.SupplierId == supplierId).OrderBy(e => e.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        return MapListToResult<SupplierEmail, SupplierEmailDto>(emails);
    }

    /// <inheritdoc />
    public async Task<Result<SupplierEmailDto>> UpdateAsync(int supplierId, int emailId, UpdateSupplierEmailRequest request, CancellationToken cancellationToken)
    {
        SupplierEmail? email = await Context.SupplierEmails.FirstOrDefaultAsync(e => e.Id == emailId && e.SupplierId == supplierId, cancellationToken).ConfigureAwait(false);
        if (email is null) return Result<SupplierEmailDto>.Failure("EMAIL_NOT_FOUND", "Supplier email not found.", 404);

        bool duplicate = await Context.SupplierEmails.AnyAsync(e => e.SupplierId == supplierId && e.EmailAddress == request.EmailAddress && e.Id != emailId, cancellationToken).ConfigureAwait(false);
        if (duplicate) return Result<SupplierEmailDto>.Failure("DUPLICATE_SUPPLIER_EMAIL", "This supplier already has this email address.", 409);

        email.EmailType = request.EmailType; email.EmailAddress = request.EmailAddress; email.ModifiedAtUtc = DateTime.UtcNow;

        if (request.IsPrimary && !email.IsPrimary)
            await PrimaryFlagHelper.UnsetOthersAsync(Context.SupplierEmails, e => e.SupplierId == supplierId && e.IsPrimary, emailId, e => e.IsPrimary = false, cancellationToken).ConfigureAwait(false);
        email.IsPrimary = request.IsPrimary;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapToResult<SupplierEmail, SupplierEmailDto>(email);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int supplierId, int emailId, CancellationToken cancellationToken)
    {
        SupplierEmail? email = await Context.SupplierEmails.FirstOrDefaultAsync(e => e.Id == emailId && e.SupplierId == supplierId, cancellationToken).ConfigureAwait(false);
        if (email is null) return Result.Failure("EMAIL_NOT_FOUND", "Supplier email not found.", 404);

        bool wasPrimary = email.IsPrimary;
        Context.SupplierEmails.Remove(email);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (wasPrimary)
        {
            await PrimaryFlagHelper.PromoteNextAsync(Context.SupplierEmails, e => e.SupplierId == supplierId, e => e.CreatedAtUtc, e => e.IsPrimary = true, cancellationToken).ConfigureAwait(false);
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
