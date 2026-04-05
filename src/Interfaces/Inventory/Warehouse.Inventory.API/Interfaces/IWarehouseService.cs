using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Interfaces;

/// <summary>
/// Defines warehouse management operations: CRUD and soft-delete.
/// <para>See <see cref="WarehouseDto"/>.</para>
/// </summary>
public interface IWarehouseService
{
    /// <summary>
    /// Gets a warehouse by ID.
    /// </summary>
    Task<Result<WarehouseDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches warehouses with pagination.
    /// </summary>
    Task<Result<PaginatedResponse<WarehouseDto>>> SearchAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new warehouse.
    /// </summary>
    Task<Result<WarehouseDto>> CreateAsync(CreateWarehouseRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing warehouse.
    /// </summary>
    Task<Result<WarehouseDto>> UpdateAsync(int id, UpdateWarehouseRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a warehouse.
    /// </summary>
    Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken);
}
