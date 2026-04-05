using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Customers.API.Interfaces;
using Warehouse.Customers.DBModel;
using Warehouse.Customers.DBModel.Models;
using Warehouse.Infrastructure.Services;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Services;

/// <summary>
/// Implements CRUD operations for customer addresses with default-flag management.
/// <para>See <see cref="ICustomerAddressService"/>, <see cref="BaseCustomerEntityService"/>.</para>
/// </summary>
public sealed class CustomerAddressService : BaseCustomerEntityService, ICustomerAddressService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public CustomerAddressService(CustomersDbContext context, IMapper mapper)
        : base(context, mapper)
    {
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

        bool isFirstOfType = !await Context.CustomerAddresses
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

        Context.CustomerAddresses.Add(address);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToResult<CustomerAddress, CustomerAddressDto>(address);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CustomerAddressDto>>> GetAddressesAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        Result? customerValidation = await ValidateCustomerExistsAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customerValidation is not null)
            return Result<IReadOnlyList<CustomerAddressDto>>.Failure(customerValidation.ErrorCode!, customerValidation.ErrorMessage!, customerValidation.StatusCode!.Value);

        List<CustomerAddress> addresses = await Context.CustomerAddresses
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderBy(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return MapListToResult<CustomerAddress, CustomerAddressDto>(addresses);
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
        {
            await PrimaryFlagHelper.UnsetOthersAsync(
                Context.CustomerAddresses,
                a => a.CustomerId == customerId && a.AddressType == request.AddressType && a.IsDefault,
                addressId,
                a => a.IsDefault = false,
                cancellationToken).ConfigureAwait(false);
        }

        address.IsDefault = request.IsDefault;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToResult<CustomerAddress, CustomerAddressDto>(address);
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

        Context.CustomerAddresses.Remove(address);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (wasDefault)
        {
            await PrimaryFlagHelper.PromoteNextAsync(
                Context.CustomerAddresses,
                a => a.CustomerId == customerId && a.AddressType == addressType,
                a => a.CreatedAtUtc,
                a => a.IsDefault = true,
                cancellationToken).ConfigureAwait(false);

            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    /// <summary>
    /// Finds an address belonging to a customer.
    /// </summary>
    private async Task<CustomerAddress?> FindAddressAsync(
        int customerId,
        int addressId,
        CancellationToken cancellationToken)
    {
        return await Context.CustomerAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId, cancellationToken)
            .ConfigureAwait(false);
    }
}
