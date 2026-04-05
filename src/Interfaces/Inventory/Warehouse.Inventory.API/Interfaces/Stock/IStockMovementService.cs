using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Interfaces.Stock;

/// <summary>
/// Defines stock movement operations: record and search.
/// <para>See <see cref="StockMovementDto"/>.</para>
/// </summary>
public interface IStockMovementService
{
    /// <summary>
    /// Records a new stock movement and updates the stock level.
    /// </summary>
    Task<Result<StockMovementDto>> RecordAsync(RecordStockMovementRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Searches stock movements with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<StockMovementDto>>> SearchAsync(SearchStockMovementsRequest request, CancellationToken cancellationToken);
}
