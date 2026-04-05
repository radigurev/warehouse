using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Interfaces.Stock;

/// <summary>
/// Defines stock level query operations: get, search, and summary.
/// <para>See <see cref="StockLevelDto"/>, <see cref="StockSummaryDto"/>.</para>
/// </summary>
public interface IStockLevelService
{
    /// <summary>
    /// Gets a stock level record by ID.
    /// </summary>
    Task<Result<StockLevelDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches stock levels with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<StockLevelDto>>> SearchAsync(SearchStockLevelsRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Gets an aggregated stock summary for a product across all warehouses.
    /// </summary>
    Task<Result<StockSummaryDto>> GetSummaryByProductAsync(int productId, CancellationToken cancellationToken);
}
