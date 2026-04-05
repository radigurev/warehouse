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
/// Implements CRUD operations for customer email entries with primary-flag and uniqueness management.
/// <para>See <see cref="ICustomerEmailService"/>, <see cref="BaseCustomerEntityService"/>.</para>
/// </summary>
public sealed class CustomerEmailService : BaseCustomerEntityService, ICustomerEmailService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public CustomerEmailService(CustomersDbContext context, IMapper mapper)
        : base(context, mapper)
    {
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

        bool isFirst = !await Context.CustomerEmails
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

        Context.CustomerEmails.Add(email);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToResult<CustomerEmail, CustomerEmailDto>(email);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CustomerEmailDto>>> GetEmailsAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        Result? customerValidation = await ValidateCustomerExistsAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customerValidation is not null)
            return Result<IReadOnlyList<CustomerEmailDto>>.Failure(customerValidation.ErrorCode!, customerValidation.ErrorMessage!, customerValidation.StatusCode!.Value);

        List<CustomerEmail> emails = await Context.CustomerEmails
            .AsNoTracking()
            .Where(e => e.CustomerId == customerId)
            .OrderBy(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return MapListToResult<CustomerEmail, CustomerEmailDto>(emails);
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
        {
            await PrimaryFlagHelper.UnsetOthersAsync(
                Context.CustomerEmails,
                e => e.CustomerId == customerId && e.IsPrimary,
                emailId,
                e => e.IsPrimary = false,
                cancellationToken).ConfigureAwait(false);
        }

        email.IsPrimary = request.IsPrimary;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToResult<CustomerEmail, CustomerEmailDto>(email);
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

        Context.CustomerEmails.Remove(email);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (wasPrimary)
        {
            await PrimaryFlagHelper.PromoteNextAsync(
                Context.CustomerEmails,
                e => e.CustomerId == customerId,
                e => e.CreatedAtUtc,
                e => e.IsPrimary = true,
                cancellationToken).ConfigureAwait(false);

            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
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
        IQueryable<CustomerEmail> query = Context.CustomerEmails
            .Where(e => e.CustomerId == customerId && e.EmailAddress == emailAddress);

        if (excludeId.HasValue)
            query = query.Where(e => e.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_CUSTOMER_EMAIL", "This customer already has this email address.", 409)
            : null;
    }

    /// <summary>
    /// Finds an email entry belonging to a customer.
    /// </summary>
    private async Task<CustomerEmail?> FindEmailAsync(
        int customerId,
        int emailId,
        CancellationToken cancellationToken)
    {
        return await Context.CustomerEmails
            .FirstOrDefaultAsync(e => e.Id == emailId && e.CustomerId == customerId, cancellationToken)
            .ConfigureAwait(false);
    }
}
