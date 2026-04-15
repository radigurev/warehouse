using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Customers.API.Interfaces;

/// <summary>
/// Defines customer lifecycle operations: CRUD, search, soft-delete, and reactivation.
/// <para>See <see cref="CustomerDetailDto"/>, <see cref="CustomerDto"/>.</para>
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Returns the next auto-generated customer code without reserving it.
    /// </summary>
    Task<Result<string>> GetNextCodeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a customer by ID with all nested data, excluding soft-deleted records.
    /// </summary>
    Task<Result<CustomerDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches customers with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<CustomerDto>>> SearchAsync(SearchCustomersRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new customer. Auto-generates code if not provided.
    /// </summary>
    Task<Result<CustomerDetailDto>> CreateAsync(CreateCustomerRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing customer's fields.
    /// </summary>
    Task<Result<CustomerDetailDto>> UpdateAsync(int id, UpdateCustomerRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a customer after verifying no active accounts with non-zero balance.
    /// </summary>
    Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Reactivates a soft-deleted customer after verifying no code or tax ID conflicts.
    /// </summary>
    Task<Result<CustomerDetailDto>> ReactivateAsync(int id, int userId, CancellationToken cancellationToken);
}
