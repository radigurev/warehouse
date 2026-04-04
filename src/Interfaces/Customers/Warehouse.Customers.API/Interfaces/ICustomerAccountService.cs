using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Interfaces;

/// <summary>
/// Defines customer account operations: CRUD, deactivation, and merge.
/// <para>See <see cref="CustomerAccountDto"/>.</para>
/// </summary>
public interface ICustomerAccountService
{
    /// <summary>
    /// Creates a new account for a customer with the specified currency.
    /// </summary>
    Task<Result<CustomerAccountDto>> CreateAsync(int customerId, CreateAccountRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all non-deleted accounts for a customer.
    /// </summary>
    Task<Result<IReadOnlyList<CustomerAccountDto>>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an account's description and primary flag.
    /// </summary>
    Task<Result<CustomerAccountDto>> UpdateAsync(int customerId, int accountId, UpdateAccountRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes an account after verifying zero balance and not the last active account.
    /// </summary>
    Task<Result> DeactivateAsync(int customerId, int accountId, CancellationToken cancellationToken);

    /// <summary>
    /// Merges two same-currency accounts belonging to the same customer within a transaction.
    /// </summary>
    Task<Result<CustomerAccountDto>> MergeAsync(int customerId, MergeAccountsRequest request, CancellationToken cancellationToken);
}
