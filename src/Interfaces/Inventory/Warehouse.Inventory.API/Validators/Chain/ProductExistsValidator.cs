using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Common.Validation;
using Warehouse.Inventory.DBModel;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators.Chain;

/// <summary>
/// Validates that the product exists and is not soft-deleted.
/// </summary>
public sealed class ProductExistsValidator : IChainValidator<RecordStockMovementRequest>
{
    private readonly InventoryDbContext _context;

    /// <summary>
    /// Initializes a new instance with the specified context.
    /// </summary>
    public ProductExistsValidator(InventoryDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public int Order => 10;

    /// <inheritdoc />
    public async Task<Result?> ValidateAsync(RecordStockMovementRequest request, CancellationToken cancellationToken)
    {
        bool exists = await _context.Products
            .AnyAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? null
            : Result.Failure("INVALID_PRODUCT", "The specified product does not exist.", 400);
    }
}
