using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Interfaces;

/// <summary>
/// Defines supplier category operations: CRUD with uniqueness and in-use checks.
/// <para>See <see cref="SupplierCategoryDto"/>.</para>
/// </summary>
public interface ISupplierCategoryService
{
    /// <summary>
    /// Creates a new supplier category with a unique name.
    /// </summary>
    Task<Result<SupplierCategoryDto>> CreateAsync(CreateSupplierCategoryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a paginated list of supplier categories.
    /// </summary>
    Task<Result<PaginatedResponse<SupplierCategoryDto>>> GetAllAsync(PaginationParams pagination, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a supplier category by ID.
    /// </summary>
    Task<Result<SupplierCategoryDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing supplier category.
    /// </summary>
    Task<Result<SupplierCategoryDto>> UpdateAsync(int id, UpdateSupplierCategoryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a category if no suppliers are assigned to it.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
