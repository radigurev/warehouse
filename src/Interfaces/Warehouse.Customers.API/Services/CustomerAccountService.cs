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
/// Implements customer account operations: CRUD, deactivation, and merge.
/// <para>See <see cref="ICustomerAccountService"/>.</para>
/// </summary>
public sealed class CustomerAccountService : ICustomerAccountService
{
    private readonly WarehouseDbContext _context;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public CustomerAccountService(WarehouseDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<CustomerAccountDto>> CreateAsync(
        int customerId,
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        Result? customerValidation = await ValidateCustomerExistsAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customerValidation is not null)
            return Result<CustomerAccountDto>.Failure(customerValidation.ErrorCode!, customerValidation.ErrorMessage!, customerValidation.StatusCode!.Value);

        Result? duplicateValidation = await ValidateNoDuplicateCurrencyAsync(customerId, request.CurrencyCode, cancellationToken).ConfigureAwait(false);
        if (duplicateValidation is not null)
            return Result<CustomerAccountDto>.Failure(duplicateValidation.ErrorCode!, duplicateValidation.ErrorMessage!, duplicateValidation.StatusCode!.Value);

        bool isFirst = !await _context.CustomerAccounts
            .AnyAsync(a => a.CustomerId == customerId && !a.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        CustomerAccount account = new()
        {
            CustomerId = customerId,
            CurrencyCode = request.CurrencyCode,
            Balance = 0m,
            Description = request.Description,
            IsPrimary = isFirst,
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.CustomerAccounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        CustomerAccountDto dto = _mapper.Map<CustomerAccountDto>(account);
        return Result<CustomerAccountDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CustomerAccountDto>>> GetByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        Result? customerValidation = await ValidateCustomerExistsAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customerValidation is not null)
            return Result<IReadOnlyList<CustomerAccountDto>>.Failure(customerValidation.ErrorCode!, customerValidation.ErrorMessage!, customerValidation.StatusCode!.Value);

        List<CustomerAccount> accounts = await _context.CustomerAccounts
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId && !a.IsDeleted)
            .OrderBy(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CustomerAccountDto> dtos = _mapper.Map<IReadOnlyList<CustomerAccountDto>>(accounts);
        return Result<IReadOnlyList<CustomerAccountDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerAccountDto>> UpdateAsync(
        int customerId,
        int accountId,
        UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        CustomerAccount? account = await _context.CustomerAccounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.CustomerId == customerId && !a.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
            return Result<CustomerAccountDto>.Failure("ACCOUNT_NOT_FOUND", "Customer account not found.", 404);

        account.Description = request.Description;

        if (request.IsPrimary && !account.IsPrimary)
            await UnsetOtherPrimaryAccountsAsync(customerId, accountId, cancellationToken).ConfigureAwait(false);

        account.IsPrimary = request.IsPrimary;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        CustomerAccountDto dto = _mapper.Map<CustomerAccountDto>(account);
        return Result<CustomerAccountDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateAsync(
        int customerId,
        int accountId,
        CancellationToken cancellationToken)
    {
        CustomerAccount? account = await _context.CustomerAccounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.CustomerId == customerId && !a.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
            return Result.Failure("ACCOUNT_NOT_FOUND", "Customer account not found.", 404);

        if (account.Balance != 0m)
            return Result.Failure("ACCOUNT_HAS_BALANCE", $"Cannot deactivate account with a non-zero balance of {account.Balance} {account.CurrencyCode}.", 409);

        int activeCount = await _context.CustomerAccounts
            .CountAsync(a => a.CustomerId == customerId && !a.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (activeCount <= 1)
            return Result.Failure("LAST_ACTIVE_ACCOUNT", "Cannot deactivate the last active account for this customer.", 409);

        account.IsDeleted = true;
        account.DeletedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<CustomerAccountDto>> MergeAsync(
        int customerId,
        MergeAccountsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SourceAccountId == request.TargetAccountId)
            return Result<CustomerAccountDto>.Failure("MERGE_SELF_NOT_ALLOWED", "Cannot merge an account into itself.", 400);

        CustomerAccount? source = await FindActiveAccountAsync(request.SourceAccountId, cancellationToken).ConfigureAwait(false);
        CustomerAccount? target = await FindActiveAccountAsync(request.TargetAccountId, cancellationToken).ConfigureAwait(false);

        Result<CustomerAccountDto>? mergeValidation = ValidateMergeAccounts(source, target, customerId);
        if (mergeValidation is not null)
            return mergeValidation;

        return await ExecuteMergeTransactionAsync(source!, target!, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the merge operation within a database transaction.
    /// </summary>
    private async Task<Result<CustomerAccountDto>> ExecuteMergeTransactionAsync(
        CustomerAccount source,
        CustomerAccount target,
        CancellationToken cancellationToken)
    {
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        target.Balance += source.Balance;
        source.Balance = 0m;
        source.IsDeleted = true;
        source.DeletedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        CustomerAccountDto dto = _mapper.Map<CustomerAccountDto>(target);
        return Result<CustomerAccountDto>.Success(dto);
    }

    /// <summary>
    /// Validates merge preconditions: both exist, same customer, same currency, both active.
    /// </summary>
    private static Result<CustomerAccountDto>? ValidateMergeAccounts(
        CustomerAccount? source,
        CustomerAccount? target,
        int customerId)
    {
        if (source is null || source.IsDeleted)
            return Result<CustomerAccountDto>.Failure("ACCOUNT_NOT_FOUND", "Customer account not found.", 404);

        if (target is null || target.IsDeleted)
            return Result<CustomerAccountDto>.Failure("ACCOUNT_NOT_FOUND", "Customer account not found.", 404);

        if (source.CustomerId != customerId || target.CustomerId != customerId)
            return Result<CustomerAccountDto>.Failure("MERGE_DIFFERENT_CUSTOMERS", "Cannot merge accounts belonging to different customers.", 400);

        if (source.CurrencyCode != target.CurrencyCode)
            return Result<CustomerAccountDto>.Failure("MERGE_CURRENCY_MISMATCH", "Cannot merge accounts with different currency codes.", 400);

        return null;
    }

    /// <summary>
    /// Finds an active account by ID.
    /// </summary>
    private async Task<CustomerAccount?> FindActiveAccountAsync(
        int accountId,
        CancellationToken cancellationToken)
    {
        return await _context.CustomerAccounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken)
            .ConfigureAwait(false);
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
    /// Validates that the customer does not already have an active account for the currency.
    /// </summary>
    private async Task<Result?> ValidateNoDuplicateCurrencyAsync(
        int customerId,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        bool exists = await _context.CustomerAccounts
            .AnyAsync(a => a.CustomerId == customerId && a.CurrencyCode == currencyCode && !a.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_CURRENCY_ACCOUNT", $"This customer already has an account with currency {currencyCode}.", 409)
            : null;
    }

    /// <summary>
    /// Unsets the IsPrimary flag on all other active accounts for the customer.
    /// </summary>
    private async Task UnsetOtherPrimaryAccountsAsync(
        int customerId,
        int excludeAccountId,
        CancellationToken cancellationToken)
    {
        List<CustomerAccount> otherPrimary = await _context.CustomerAccounts
            .Where(a => a.CustomerId == customerId && a.Id != excludeAccountId && a.IsPrimary && !a.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (CustomerAccount account in otherPrimary)
            account.IsPrimary = false;
    }
}
