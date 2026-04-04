using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Interfaces;

/// <summary>
/// Defines customer category operations: CRUD with uniqueness and in-use checks.
/// <para>See <see cref="CustomerCategoryDto"/>.</para>
/// </summary>
public interface ICustomerCategoryService
{
    /// <summary>
    /// Creates a new customer category with a unique name.
    /// </summary>
    Task<Result<CustomerCategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all customer categories.
    /// </summary>
    Task<Result<IReadOnlyList<CustomerCategoryDto>>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a customer category by ID.
    /// </summary>
    Task<Result<CustomerCategoryDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing customer category.
    /// </summary>
    Task<Result<CustomerCategoryDto>> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a category if no customers are assigned to it.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
