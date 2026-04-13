using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Interfaces.Stock;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Services.Stock;

/// <summary>
/// Centralizes all stock level lookups, creation, availability checks, and quantity adjustments.
/// Does not call SaveChangesAsync or publish events — callers own the unit of work.
/// <para>See <see cref="IStockLevelManager"/>, <see cref="StockLevel"/>.</para>
/// </summary>
public sealed class StockLevelManager : IStockLevelManager
{
    private readonly InventoryDbContext _context;

    /// <summary>
    /// Initializes a new instance with the specified inventory database context.
    /// </summary>
    public StockLevelManager(InventoryDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<StockLevel> GetOrCreateAsync(
        int productId,
        int warehouseId,
        int? locationId,
        int? batchId,
        CancellationToken ct)
    {
        StockLevel? existing = await FindStockLevelAsync(productId, warehouseId, locationId, batchId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
            return existing;

        StockLevel created = new()
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            LocationId = locationId,
            BatchId = batchId,
            QuantityOnHand = 0m,
            QuantityReserved = 0m
        };

        _context.StockLevels.Add(created);
        return created;
    }

    /// <inheritdoc />
    public async Task<Result?> AdjustQuantityAsync(
        int productId,
        int warehouseId,
        int? locationId,
        int? batchId,
        decimal adjustment,
        CancellationToken ct)
    {
        StockLevel stockLevel = await GetOrCreateAsync(productId, warehouseId, locationId, batchId, ct)
            .ConfigureAwait(false);

        if (adjustment < 0 && stockLevel.QuantityOnHand + adjustment < 0)
        {
            return Result.Failure(
                "INSUFFICIENT_STOCK",
                $"Insufficient stock for product {productId}. Available: {stockLevel.QuantityOnHand}, requested: {Math.Abs(adjustment)}.",
                409);
        }

        stockLevel.QuantityOnHand += adjustment;
        stockLevel.ModifiedAtUtc = DateTime.UtcNow;

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> HasSufficientStockAsync(
        int productId,
        int warehouseId,
        int? locationId,
        decimal requiredQuantity,
        CancellationToken ct)
    {
        decimal available = await GetAvailableQuantityAsync(productId, warehouseId, locationId, ct)
            .ConfigureAwait(false);

        return available >= requiredQuantity;
    }

    /// <inheritdoc />
    public async Task<decimal> GetAvailableQuantityAsync(
        int productId,
        int warehouseId,
        int? locationId,
        CancellationToken ct)
    {
        StockLevel? stockLevel = await _context.StockLevels
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId &&
                s.LocationId == locationId,
                ct)
            .ConfigureAwait(false);

        if (stockLevel is null)
            return 0m;

        return stockLevel.QuantityOnHand;
    }

    /// <summary>
    /// Finds an existing stock level by the composite key of product, warehouse, location, and batch.
    /// </summary>
    private async Task<StockLevel?> FindStockLevelAsync(
        int productId,
        int warehouseId,
        int? locationId,
        int? batchId,
        CancellationToken ct)
    {
        return await _context.StockLevels
            .FirstOrDefaultAsync(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId &&
                s.LocationId == locationId &&
                s.BatchId == batchId,
                ct)
            .ConfigureAwait(false);
    }
}
