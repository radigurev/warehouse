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
/// Implements CRUD operations for customer phone entries with primary-flag management.
/// <para>See <see cref="ICustomerPhoneService"/>, <see cref="BaseCustomerEntityService"/>.</para>
/// </summary>
public sealed class CustomerPhoneService : BaseCustomerEntityService, ICustomerPhoneService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public CustomerPhoneService(CustomersDbContext context, IMapper mapper)
        : base(context, mapper)
    {
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

        bool isFirst = !await Context.CustomerPhones
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

        Context.CustomerPhones.Add(phone);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToResult<CustomerPhone, CustomerPhoneDto>(phone);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CustomerPhoneDto>>> GetPhonesAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        Result? customerValidation = await ValidateCustomerExistsAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customerValidation is not null)
            return Result<IReadOnlyList<CustomerPhoneDto>>.Failure(customerValidation.ErrorCode!, customerValidation.ErrorMessage!, customerValidation.StatusCode!.Value);

        List<CustomerPhone> phones = await Context.CustomerPhones
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderBy(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return MapListToResult<CustomerPhone, CustomerPhoneDto>(phones);
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
        {
            await PrimaryFlagHelper.UnsetOthersAsync(
                Context.CustomerPhones,
                p => p.CustomerId == customerId && p.IsPrimary,
                phoneId,
                p => p.IsPrimary = false,
                cancellationToken).ConfigureAwait(false);
        }

        phone.IsPrimary = request.IsPrimary;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToResult<CustomerPhone, CustomerPhoneDto>(phone);
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

        Context.CustomerPhones.Remove(phone);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (wasPrimary)
        {
            await PrimaryFlagHelper.PromoteNextAsync(
                Context.CustomerPhones,
                p => p.CustomerId == customerId,
                p => p.CreatedAtUtc,
                p => p.IsPrimary = true,
                cancellationToken).ConfigureAwait(false);

            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    /// <summary>
    /// Finds a phone entry belonging to a customer.
    /// </summary>
    private async Task<CustomerPhone?> FindPhoneAsync(
        int customerId,
        int phoneId,
        CancellationToken cancellationToken)
    {
        return await Context.CustomerPhones
            .FirstOrDefaultAsync(p => p.Id == phoneId && p.CustomerId == customerId, cancellationToken)
            .ConfigureAwait(false);
    }
}
