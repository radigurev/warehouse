using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Interfaces;

/// <summary>
/// Defines supplier lifecycle operations: CRUD, search, soft-delete, and reactivation.
/// <para>See <see cref="SupplierDetailDto"/>, <see cref="SupplierDto"/>.</para>
/// </summary>
public interface ISupplierService
{
    /// <summary>
    /// Returns the next auto-generated supplier code without reserving it.
    /// </summary>
    Task<Result<string>> GetNextCodeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a supplier by ID with all nested data, excluding soft-deleted records.
    /// </summary>
    Task<Result<SupplierDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches suppliers with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<SupplierDto>>> SearchAsync(SearchSuppliersRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new supplier. Auto-generates code if not provided.
    /// </summary>
    Task<Result<SupplierDetailDto>> CreateAsync(CreateSupplierRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing supplier's fields.
    /// </summary>
    Task<Result<SupplierDetailDto>> UpdateAsync(int id, UpdateSupplierRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a supplier after verifying no open purchase orders.
    /// </summary>
    Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Reactivates a soft-deleted supplier after verifying no code or tax ID conflicts.
    /// </summary>
    Task<Result<SupplierDetailDto>> ReactivateAsync(int id, int userId, CancellationToken cancellationToken);
}
