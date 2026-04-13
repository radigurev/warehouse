using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Common.Validation;
using Warehouse.Inventory.DBModel;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators.Chain;

/// <summary>
/// Validates that the warehouse exists and is not soft-deleted.
/// </summary>
public sealed class WarehouseExistsValidator : IChainValidator<RecordStockMovementRequest>
{
    private readonly InventoryDbContext _context;

    /// <summary>
    /// Initializes a new instance with the specified context.
    /// </summary>
    public WarehouseExistsValidator(InventoryDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public int Order => 30;

    /// <inheritdoc />
    public async Task<Result?> ValidateAsync(RecordStockMovementRequest request, CancellationToken cancellationToken)
    {
        bool exists = await _context.Warehouses
            .AnyAsync(w => w.Id == request.WarehouseId && !w.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? null
            : Result.Failure("INVALID_WAREHOUSE", "The specified warehouse does not exist.", 400);
    }
}
