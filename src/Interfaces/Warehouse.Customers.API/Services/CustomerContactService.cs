using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Customers.API.Interfaces;
using Warehouse.DBModel;
using Warehouse.DBModel.Models.Customers;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Services;

/// <summary>
/// Implements contact information operations for addresses, phones, and emails.
/// <para>See <see cref="ICustomerContactService"/>.</para>
/// </summary>
public sealed class CustomerContactService : ICustomerContactService
{
    private readonly WarehouseDbContext _context;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public CustomerContactService(WarehouseDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<CustomerAddressDto>> CreateAddressAsync(
        int customerId,
        CreateAddressRequest request,
        CancellationToken cancellationToken)
    {
        Result? customerValidation = await ValidateCustomerExistsAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customerValidation is not null)
            return Result<CustomerAddressDto>.Failure(customerValidation.ErrorCode!, customerValidation.ErrorMessage!, customerValidation.StatusCode!.Value);

        bool isFirstOfType = !await _context.CustomerAddresses
            .AnyAsync(a => a.CustomerId == customerId && a.AddressType == request.AddressType, cancellationToken)
            .ConfigureAwait(false);

        CustomerAddress address = new()
        {
            CustomerId = customerId,
            AddressType = request.AddressType,
            StreetLine1 = request.StreetLine1,
            StreetLine2 = request.StreetLine2,
            City = request.City,
            StateProvince = request.StateProvince,
            PostalCode = request.PostalCode,
            CountryCode = request.CountryCode,
            IsDefault = isFirstOfType,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.CustomerAddresses.Add(address);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        CustomerAddressDto dto = _mapper.Map<CustomerAddressDto>(address);
        return Result<CustomerAddressDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CustomerAddressDto>>> GetAddressesAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        Result? customerValidation = await ValidateCustomerExistsAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customerValidation is not null)
            return Result<IReadOnlyList<CustomerAddressDto>>.Failure(customerValidation.ErrorCode!, customerValidation.ErrorMessage!, customerValidation.StatusCode!.Value);

        List<CustomerAddress> addresses = await _context.CustomerAddresses
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderBy(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CustomerAddressDto> dtos = _mapper.Map<IReadOnlyList<CustomerAddressDto>>(addresses);
        return Result<IReadOnlyList<CustomerAddressDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerAddressDto>> UpdateAddressAsync(
        int customerId,
        int addressId,
        UpdateAddressRequest request,
        CancellationToken cancellationToken)
    {
        CustomerAddress? address = await FindAddressAsync(customerId, addressId, cancellationToken).ConfigureAwait(false);
        if (address is null)
            return Result<CustomerAddressDto>.Failure("ADDRESS_NOT_FOUND", "Customer address not found.", 404);

        address.AddressType = request.AddressType;
        address.StreetLine1 = request.StreetLine1;
        address.StreetLine2 = request.StreetLine2;
        address.City = request.City;
        address.StateProvince = request.StateProvince;
        address.PostalCode = request.PostalCode;
        address.CountryCode = request.CountryCode;
        address.ModifiedAtUtc = DateTime.UtcNow;

        if (request.IsDefault && !address.IsDefault)
            await UnsetOtherDefaultAddressesAsync(customerId, request.AddressType, addressId, cancellationToken).ConfigureAwait(false);

        address.IsDefault = request.IsDefault;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        CustomerAddressDto dto = _mapper.Map<CustomerAddressDto>(address);
        return Result<CustomerAddressDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAddressAsync(
        int customerId,
        int addressId,
        CancellationToken cancellationToken)
    {
        CustomerAddress? address = await FindAddressAsync(customerId, addressId, cancellationToken).ConfigureAwait(false);
        if (address is null)
            return Result.Failure("ADDRESS_NOT_FOUND", "Customer address not found.", 404);

        bool wasDefault = address.IsDefault;
        string addressType = address.AddressType;

        _context.CustomerAddresses.Remove(address);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (wasDefault)
            await PromoteNextDefaultAddressAsync(customerId, addressType, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<CustomerPhoneDto>> CreatePhoneAsync(
        int customerId,
        CreatePhoneRequest request,
        CancellationToken cancellationToken)
    {
        Result? customerValidation = await ValidateCustomerExistsAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customerValidation is not null)
            return Result<CustomerPhoneDto>.Failure(customerValidation.ErrorCode!, customerValidation.ErrorMessage!, customerValidation.StatusCode!.Value);

        bool isFirst = !await _context.CustomerPhones
            .AnyAsync(p => p.CustomerId == customerId, cancellationToken)
            .ConfigureAwait(false);

        CustomerPhone phone = new()
        {
            CustomerId = customerId,
            PhoneType = request.PhoneType,
            PhoneNumber = request.PhoneNumber,
            Extension = request.Extension,
            IsPrimary = isFirst,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.CustomerPhones.Add(phone);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        CustomerPhoneDto dto = _mapper.Map<CustomerPhoneDto>(phone);
        return Result<CustomerPhoneDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CustomerPhoneDto>>> GetPhonesAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        Result? customerValidation = await ValidateCustomerExistsAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customerValidation is not null)
            return Result<IReadOnlyList<CustomerPhoneDto>>.Failure(customerValidation.ErrorCode!, customerValidation.ErrorMessage!, customerValidation.StatusCode!.Value);

        List<CustomerPhone> phones = await _context.CustomerPhones
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderBy(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CustomerPhoneDto> dtos = _mapper.Map<IReadOnlyList<CustomerPhoneDto>>(phones);
        return Result<IReadOnlyList<CustomerPhoneDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerPhoneDto>> UpdatePhoneAsync(
        int customerId,
        int phoneId,
        UpdatePhoneRequest request,
        CancellationToken cancellationToken)
    {
        CustomerPhone? phone = await FindPhoneAsync(customerId, phoneId, cancellationToken).ConfigureAwait(false);
        if (phone is null)
            return Result<CustomerPhoneDto>.Failure("PHONE_NOT_FOUND", "Customer phone not found.", 404);

        phone.PhoneType = request.PhoneType;
        phone.PhoneNumber = request.PhoneNumber;
        phone.Extension = request.Extension;
        phone.ModifiedAtUtc = DateTime.UtcNow;

        if (request.IsPrimary && !phone.IsPrimary)
            await UnsetOtherPrimaryPhonesAsync(customerId, phoneId, cancellationToken).ConfigureAwait(false);

        phone.IsPrimary = request.IsPrimary;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        CustomerPhoneDto dto = _mapper.Map<CustomerPhoneDto>(phone);
        return Result<CustomerPhoneDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeletePhoneAsync(
        int customerId,
        int phoneId,
        CancellationToken cancellationToken)
    {
        CustomerPhone? phone = await FindPhoneAsync(customerId, phoneId, cancellationToken).ConfigureAwait(false);
        if (phone is null)
            return Result.Failure("PHONE_NOT_FOUND", "Customer phone not found.", 404);

        bool wasPrimary = phone.IsPrimary;

        _context.CustomerPhones.Remove(phone);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (wasPrimary)
            await PromoteNextPrimaryPhoneAsync(customerId, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<CustomerEmailDto>> CreateEmailAsync(
        int customerId,
        CreateEmailRequest request,
        CancellationToken cancellationToken)
    {
        Result? customerValidation = await ValidateCustomerExistsAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customerValidation is not null)
            return Result<CustomerEmailDto>.Failure(customerValidation.ErrorCode!, customerValidation.ErrorMessage!, customerValidation.StatusCode!.Value);

        Result? duplicateValidation = await ValidateUniqueEmailAsync(customerId, request.EmailAddress, null, cancellationToken).ConfigureAwait(false);
        if (duplicateValidation is not null)
            return Result<CustomerEmailDto>.Failure(duplicateValidation.ErrorCode!, duplicateValidation.ErrorMessage!, duplicateValidation.StatusCode!.Value);

        bool isFirst = !await _context.CustomerEmails
            .AnyAsync(e => e.CustomerId == customerId, cancellationToken)
            .ConfigureAwait(false);

        CustomerEmail email = new()
        {
            CustomerId = customerId,
            EmailType = request.EmailType,
            EmailAddress = request.EmailAddress,
            IsPrimary = isFirst,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.CustomerEmails.Add(email);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        CustomerEmailDto dto = _mapper.Map<CustomerEmailDto>(email);
        return Result<CustomerEmailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CustomerEmailDto>>> GetEmailsAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        Result? customerValidation = await ValidateCustomerExistsAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customerValidation is not null)
            return Result<IReadOnlyList<CustomerEmailDto>>.Failure(customerValidation.ErrorCode!, customerValidation.ErrorMessage!, customerValidation.StatusCode!.Value);

        List<CustomerEmail> emails = await _context.CustomerEmails
            .AsNoTracking()
            .Where(e => e.CustomerId == customerId)
            .OrderBy(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CustomerEmailDto> dtos = _mapper.Map<IReadOnlyList<CustomerEmailDto>>(emails);
        return Result<IReadOnlyList<CustomerEmailDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerEmailDto>> UpdateEmailAsync(
        int customerId,
        int emailId,
        UpdateEmailRequest request,
        CancellationToken cancellationToken)
    {
        CustomerEmail? email = await FindEmailAsync(customerId, emailId, cancellationToken).ConfigureAwait(false);
        if (email is null)
            return Result<CustomerEmailDto>.Failure("EMAIL_NOT_FOUND", "Customer email not found.", 404);

        Result? duplicateValidation = await ValidateUniqueEmailAsync(customerId, request.EmailAddress, emailId, cancellationToken).ConfigureAwait(false);
        if (duplicateValidation is not null)
            return Result<CustomerEmailDto>.Failure(duplicateValidation.ErrorCode!, duplicateValidation.ErrorMessage!, duplicateValidation.StatusCode!.Value);

        email.EmailType = request.EmailType;
        email.EmailAddress = request.EmailAddress;
        email.ModifiedAtUtc = DateTime.UtcNow;

        if (request.IsPrimary && !email.IsPrimary)
            await UnsetOtherPrimaryEmailsAsync(customerId, emailId, cancellationToken).ConfigureAwait(false);

        email.IsPrimary = request.IsPrimary;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        CustomerEmailDto dto = _mapper.Map<CustomerEmailDto>(email);
        return Result<CustomerEmailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteEmailAsync(
        int customerId,
        int emailId,
        CancellationToken cancellationToken)
    {
        CustomerEmail? email = await FindEmailAsync(customerId, emailId, cancellationToken).ConfigureAwait(false);
        if (email is null)
            return Result.Failure("EMAIL_NOT_FOUND", "Customer email not found.", 404);

        bool wasPrimary = email.IsPrimary;

        _context.CustomerEmails.Remove(email);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (wasPrimary)
            await PromoteNextPrimaryEmailAsync(customerId, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    /// <summary>
    /// Validates that a customer exists and is not soft-deleted.
    /// </summary>
    private async Task<Result?> ValidateCustomerExistsAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        bool exists = await _context.Customers
            .AnyAsync(c => c.Id == customerId && !c.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? null
            : Result.Failure("CUSTOMER_NOT_FOUND", "Customer not found.", 404);
    }

    /// <summary>
    /// Validates email address uniqueness within a customer.
    /// </summary>
    private async Task<Result?> ValidateUniqueEmailAsync(
        int customerId,
        string emailAddress,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<CustomerEmail> query = _context.CustomerEmails
            .Where(e => e.CustomerId == customerId && e.EmailAddress == emailAddress);

        if (excludeId.HasValue)
            query = query.Where(e => e.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_CUSTOMER_EMAIL", "This customer already has this email address.", 409)
            : null;
    }

    /// <summary>
    /// Finds an address belonging to a customer.
    /// </summary>
    private async Task<CustomerAddress?> FindAddressAsync(
        int customerId,
        int addressId,
        CancellationToken cancellationToken)
    {
        return await _context.CustomerAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Finds a phone entry belonging to a customer.
    /// </summary>
    private async Task<CustomerPhone?> FindPhoneAsync(
        int customerId,
        int phoneId,
        CancellationToken cancellationToken)
    {
        return await _context.CustomerPhones
            .FirstOrDefaultAsync(p => p.Id == phoneId && p.CustomerId == customerId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Finds an email entry belonging to a customer.
    /// </summary>
    private async Task<CustomerEmail?> FindEmailAsync(
        int customerId,
        int emailId,
        CancellationToken cancellationToken)
    {
        return await _context.CustomerEmails
            .FirstOrDefaultAsync(e => e.Id == emailId && e.CustomerId == customerId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Unsets IsDefault on other addresses of the same type for the customer.
    /// </summary>
    private async Task UnsetOtherDefaultAddressesAsync(
        int customerId,
        string addressType,
        int excludeAddressId,
        CancellationToken cancellationToken)
    {
        List<CustomerAddress> others = await _context.CustomerAddresses
            .Where(a => a.CustomerId == customerId && a.AddressType == addressType && a.Id != excludeAddressId && a.IsDefault)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (CustomerAddress address in others)
            address.IsDefault = false;
    }

    /// <summary>
    /// Promotes the next address of the same type to default by creation date.
    /// </summary>
    private async Task PromoteNextDefaultAddressAsync(
        int customerId,
        string addressType,
        CancellationToken cancellationToken)
    {
        CustomerAddress? next = await _context.CustomerAddresses
            .Where(a => a.CustomerId == customerId && a.AddressType == addressType)
            .OrderBy(a => a.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (next is not null)
        {
            next.IsDefault = true;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Unsets IsPrimary on other phones for the customer.
    /// </summary>
    private async Task UnsetOtherPrimaryPhonesAsync(
        int customerId,
        int excludePhoneId,
        CancellationToken cancellationToken)
    {
        List<CustomerPhone> others = await _context.CustomerPhones
            .Where(p => p.CustomerId == customerId && p.Id != excludePhoneId && p.IsPrimary)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (CustomerPhone phone in others)
            phone.IsPrimary = false;
    }

    /// <summary>
    /// Promotes the next phone to primary by creation date.
    /// </summary>
    private async Task PromoteNextPrimaryPhoneAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        CustomerPhone? next = await _context.CustomerPhones
            .Where(p => p.CustomerId == customerId)
            .OrderBy(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (next is not null)
        {
            next.IsPrimary = true;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Unsets IsPrimary on other emails for the customer.
    /// </summary>
    private async Task UnsetOtherPrimaryEmailsAsync(
        int customerId,
        int excludeEmailId,
        CancellationToken cancellationToken)
    {
        List<CustomerEmail> others = await _context.CustomerEmails
            .Where(e => e.CustomerId == customerId && e.Id != excludeEmailId && e.IsPrimary)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (CustomerEmail email in others)
            email.IsPrimary = false;
    }

    /// <summary>
    /// Promotes the next email to primary by creation date.
    /// </summary>
    private async Task PromoteNextPrimaryEmailAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        CustomerEmail? next = await _context.CustomerEmails
            .Where(e => e.CustomerId == customerId)
            .OrderBy(e => e.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (next is not null)
        {
            next.IsPrimary = true;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
