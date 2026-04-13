using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Common.Validation;
using Warehouse.Inventory.DBModel;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators.Chain;

/// <summary>
/// Validates that products requiring batch tracking have a BatchId provided.
/// </summary>
public sealed class BatchTrackingValidator : IChainValidator<RecordStockMovementRequest>
{
    private readonly InventoryDbContext _context;

    /// <summary>
    /// Initializes a new instance with the specified context.
    /// </summary>
    public BatchTrackingValidator(InventoryDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public int Order => 20;

    /// <inheritdoc />
    public async Task<Result?> ValidateAsync(RecordStockMovementRequest request, CancellationToken cancellationToken)
    {
        bool requiresBatch = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == request.ProductId && !p.IsDeleted)
            .Select(p => p.RequiresBatchTracking)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (requiresBatch && request.BatchId is null)
            return Result.Failure("BATCH_REQUIRED", "This product requires batch tracking. A BatchId must be provided.", 400);

        return null;
    }
}
