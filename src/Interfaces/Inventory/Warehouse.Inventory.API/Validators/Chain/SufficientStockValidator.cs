using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Common.Validation;
using Warehouse.Inventory.DBModel;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators.Chain;

/// <summary>
/// Validates that sufficient stock exists for outbound movements (negative quantity).
/// Skips validation for inbound movements (positive quantity).
/// </summary>
public sealed class SufficientStockValidator : IChainValidator<RecordStockMovementRequest>
{
    private readonly InventoryDbContext _context;

    /// <summary>
    /// Initializes a new instance with the specified context.
    /// </summary>
    public SufficientStockValidator(InventoryDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public int Order => 40;

    /// <inheritdoc />
    public async Task<Result?> ValidateAsync(RecordStockMovementRequest request, CancellationToken cancellationToken)
    {
        if (request.Quantity >= 0)
            return null;

        Warehouse.Inventory.DBModel.Models.StockLevel? stockLevel = await _context.StockLevels
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.ProductId == request.ProductId &&
                s.WarehouseId == request.WarehouseId &&
                s.LocationId == request.LocationId &&
                s.BatchId == request.BatchId,
                cancellationToken)
            .ConfigureAwait(false);

        decimal available = stockLevel?.QuantityOnHand ?? 0;

        if (available < Math.Abs(request.Quantity))
            return Result.Failure("INSUFFICIENT_STOCK", "Insufficient stock for this movement.", 409);

        return null;
    }
}
