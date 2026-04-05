using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Interfaces;

/// <summary>
/// Defines warehouse transfer operations: create, complete, cancel, get, and search.
/// <para>See <see cref="WarehouseTransferDto"/>, <see cref="WarehouseTransferDetailDto"/>.</para>
/// </summary>
public interface IWarehouseTransferService
{
    /// <summary>
    /// Gets a transfer by ID with lines.
    /// </summary>
    Task<Result<WarehouseTransferDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches transfers with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<WarehouseTransferDto>>> SearchAsync(SearchTransfersRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new transfer in Draft status.
    /// </summary>
    Task<Result<WarehouseTransferDetailDto>> CreateAsync(CreateTransferRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Completes a draft transfer and moves stock.
    /// </summary>
    Task<Result<WarehouseTransferDetailDto>> CompleteAsync(int id, CompleteTransferRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a draft transfer.
    /// </summary>
    Task<Result> CancelAsync(int id, CancellationToken cancellationToken);
}
