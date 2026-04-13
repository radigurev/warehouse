using Warehouse.Common.Models;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Interfaces.Stock;

/// <summary>
/// Defines the single entry point for all stock level queries and mutations.
/// <para>See <see cref="StockLevel"/>.</para>
/// </summary>
public interface IStockLevelManager
{
    /// <summary>
    /// Finds an existing stock level or creates a new one with zero quantity.
    /// </summary>
    Task<StockLevel> GetOrCreateAsync(int productId, int warehouseId, int? locationId, int? batchId, CancellationToken ct);

    /// <summary>
    /// Adjusts the on-hand quantity. Returns a failure result if stock is insufficient for negative adjustments.
    /// </summary>
    Task<Result?> AdjustQuantityAsync(int productId, int warehouseId, int? locationId, int? batchId, decimal adjustment, CancellationToken ct);

    /// <summary>
    /// Checks whether the available on-hand quantity meets or exceeds the required amount.
    /// </summary>
    Task<bool> HasSufficientStockAsync(int productId, int warehouseId, int? locationId, decimal requiredQuantity, CancellationToken ct);

    /// <summary>
    /// Returns the current on-hand quantity, or zero if no stock level record exists.
    /// </summary>
    Task<decimal> GetAvailableQuantityAsync(int productId, int warehouseId, int? locationId, CancellationToken ct);
}
