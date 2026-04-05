using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Interfaces;

/// <summary>
/// Defines storage location lifecycle operations: CRUD and search.
/// <para>See <see cref="StorageLocationDto"/>.</para>
/// </summary>
public interface IStorageLocationService
{
    /// <summary>
    /// Gets a storage location by ID.
    /// </summary>
    Task<Result<StorageLocationDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches storage locations with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<StorageLocationDto>>> SearchAsync(SearchStorageLocationsRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new storage location within a zone.
    /// </summary>
    Task<Result<StorageLocationDto>> CreateAsync(CreateStorageLocationRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing storage location.
    /// </summary>
    Task<Result<StorageLocationDto>> UpdateAsync(int id, UpdateStorageLocationRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a storage location by ID.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
