using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Interfaces.Stock;

/// <summary>
/// Defines batch lifecycle operations: CRUD and search.
/// <para>See <see cref="BatchDto"/>.</para>
/// </summary>
public interface IBatchService
{
    /// <summary>
    /// Gets a batch by ID.
    /// </summary>
    Task<Result<BatchDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches batches with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<BatchDto>>> SearchAsync(SearchBatchesRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new batch for a product.
    /// </summary>
    Task<Result<BatchDto>> CreateAsync(CreateBatchRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing batch.
    /// </summary>
    Task<Result<BatchDto>> UpdateAsync(int id, UpdateBatchRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Deactivates a batch.
    /// </summary>
    Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken);
}
