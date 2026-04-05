using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Interfaces.Stock;

/// <summary>
/// Defines inventory adjustment operations: create, approve, reject, apply, get, and search.
/// <para>See <see cref="InventoryAdjustmentDto"/>, <see cref="InventoryAdjustmentDetailDto"/>.</para>
/// </summary>
public interface IInventoryAdjustmentService
{
    /// <summary>
    /// Gets an adjustment by ID with lines and approval details.
    /// </summary>
    Task<Result<InventoryAdjustmentDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches adjustments with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<InventoryAdjustmentDto>>> SearchAsync(SearchAdjustmentsRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new adjustment in Pending status.
    /// </summary>
    Task<Result<InventoryAdjustmentDetailDto>> CreateAsync(CreateAdjustmentRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Approves a pending adjustment.
    /// </summary>
    Task<Result<InventoryAdjustmentDetailDto>> ApproveAsync(int id, ApproveAdjustmentRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Rejects a pending adjustment.
    /// </summary>
    Task<Result<InventoryAdjustmentDetailDto>> RejectAsync(int id, RejectAdjustmentRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Applies an approved adjustment to stock levels.
    /// </summary>
    Task<Result<InventoryAdjustmentDetailDto>> ApplyAsync(int id, int userId, CancellationToken cancellationToken);
}
