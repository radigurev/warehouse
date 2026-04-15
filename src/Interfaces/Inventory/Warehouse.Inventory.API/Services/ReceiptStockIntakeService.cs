using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Warehouse.Common.Enums;
using Warehouse.Infrastructure.Sequences;
using Warehouse.Inventory.API.Interfaces;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.API.Services;

/// <summary>
/// Implements the receipt stock intake pipeline: batch resolution, immutable stock movement
/// creation, and stock level upsert. Idempotent per goods receipt line.
/// <para>Specification: SDD-INV-005.</para>
/// </summary>
public sealed class ReceiptStockIntakeService : IReceiptStockIntakeService
{
    private readonly InventoryDbContext _context;
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly ILogger<ReceiptStockIntakeService> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public ReceiptStockIntakeService(
        InventoryDbContext context,
        ISequenceGenerator sequenceGenerator,
        ILogger<ReceiptStockIntakeService> logger)
    {
        _context = context;
        _sequenceGenerator = sequenceGenerator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ProcessLineAsync(ReceiptLineContext context, CancellationToken cancellationToken)
    {
        if (!ValidateLine(context))
            return false;

        bool isDuplicate = await CheckIdempotencyAsync(context.GoodsReceiptLineId, cancellationToken).ConfigureAwait(false);
        if (isDuplicate)
        {
            _logger.LogInformation(
                "Duplicate receipt line skipped: GoodsReceiptLineId={GoodsReceiptLineId}",
                context.GoodsReceiptLineId);
            return false;
        }

        Product? product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == context.ProductId && !p.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            _logger.LogError(
                "Product not found in inventory: ProductId={ProductId}, GoodsReceiptLineId={GoodsReceiptLineId}",
                context.ProductId, context.GoodsReceiptLineId);
            return false;
        }

        int? batchId = await ResolveBatchAsync(context, product, cancellationToken).ConfigureAwait(false);

        StockMovement movement = CreateMovement(context, batchId);
        _context.StockMovements.Add(movement);

        await UpsertStockLevelAsync(context, batchId, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Validates line-level fields per SDD-INV-005 Section 3.2.
    /// </summary>
    private bool ValidateLine(ReceiptLineContext context)
    {
        if (context.GoodsReceiptLineId <= 0)
        {
            _logger.LogError("Invalid GoodsReceiptLineId: {GoodsReceiptLineId}", context.GoodsReceiptLineId);
            return false;
        }

        if (context.ProductId <= 0)
        {
            _logger.LogError(
                "Invalid ProductId: {ProductId}, GoodsReceiptLineId={GoodsReceiptLineId}",
                context.ProductId, context.GoodsReceiptLineId);
            return false;
        }

        if (context.Quantity <= 0)
        {
            _logger.LogError(
                "Invalid Quantity: {Quantity}, GoodsReceiptLineId={GoodsReceiptLineId}",
                context.Quantity, context.GoodsReceiptLineId);
            return false;
        }

        if (context.BatchNumber is not null && context.BatchNumber.Length > 50)
        {
            _logger.LogError(
                "BatchNumber exceeds 50 characters: GoodsReceiptLineId={GoodsReceiptLineId}",
                context.GoodsReceiptLineId);
            return false;
        }

        if (context.ManufacturingDate.HasValue && context.ExpiryDate.HasValue
            && context.ExpiryDate.Value <= context.ManufacturingDate.Value)
        {
            _logger.LogError(
                "ExpiryDate is before ManufacturingDate: GoodsReceiptLineId={GoodsReceiptLineId}",
                context.GoodsReceiptLineId);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks whether a stock movement already exists for this goods receipt line (idempotency).
    /// </summary>
    private async Task<bool> CheckIdempotencyAsync(int goodsReceiptLineId, CancellationToken cancellationToken)
    {
        return await _context.StockMovements
            .AnyAsync(m =>
                m.ReferenceType == StockMovementReferenceType.PurchaseOrder &&
                m.ReferenceId == goodsReceiptLineId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves or creates a batch according to SDD-INV-005 Section 2.2.
    /// Every receipt MUST produce a Material Lot per ISA-95 Part 2 Section 7 (compliance rule 5).
    /// When no batch number is provided, one is auto-generated regardless of the product tracking flag.
    /// </summary>
    private async Task<int?> ResolveBatchAsync(
        ReceiptLineContext context,
        Product product,
        CancellationToken cancellationToken)
    {
        bool hasBatchNumber = !string.IsNullOrWhiteSpace(context.BatchNumber);

        if (hasBatchNumber)
            return await FindOrCreateBatchAsync(context, context.BatchNumber!, cancellationToken).ConfigureAwait(false);

        string generatedNumber = await GenerateBatchNumberAsync(product.Code, context.GoodsReceiptLineId, cancellationToken).ConfigureAwait(false);
        return await FindOrCreateBatchAsync(context, generatedNumber, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Finds an existing batch by product and number, or creates a new one.
    /// Handles concurrent insert conflicts by re-querying.
    /// </summary>
    private async Task<int> FindOrCreateBatchAsync(
        ReceiptLineContext context,
        string batchNumber,
        CancellationToken cancellationToken)
    {
        Batch? existing = await _context.Batches
            .FirstOrDefaultAsync(b =>
                b.ProductId == context.ProductId &&
                b.BatchNumber == batchNumber,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
            return existing.Id;

        Batch batch = new()
        {
            ProductId = context.ProductId,
            BatchNumber = batchNumber,
            ManufacturingDate = context.ManufacturingDate,
            ExpiryDate = context.ExpiryDate,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = context.CreatedByUserId
        };

        _context.Batches.Add(batch);

        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return batch.Id;
        }
        catch (DbUpdateException)
        {
            _logger.LogWarning(
                "Batch unique constraint conflict, re-querying: ProductId={ProductId}, BatchNumber={BatchNumber}",
                context.ProductId, batchNumber);

            _context.Entry(batch).State = EntityState.Detached;

            Batch? conflictBatch = await _context.Batches
                .FirstOrDefaultAsync(b =>
                    b.ProductId == context.ProductId &&
                    b.BatchNumber == batchNumber,
                    cancellationToken)
                .ConfigureAwait(false);

            if (conflictBatch is not null)
                return conflictBatch.Id;

            throw;
        }
    }

    /// <summary>
    /// Generates a batch number using the sequence generator, with fallback per SDD-INV-005 Section 2.2.2.
    /// </summary>
    private async Task<string> GenerateBatchNumberAsync(
        string productCode,
        int goodsReceiptLineId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _sequenceGenerator.NextBatchNumberAsync(productCode, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ISequenceGenerator unavailable, using fallback batch number format: ProductCode={ProductCode}",
                productCode);
            return $"{productCode}-{DateTime.UtcNow:yyyyMMdd}-{goodsReceiptLineId}";
        }
    }

    /// <summary>
    /// Creates an immutable stock movement record per SDD-INV-005 Section 2.3.
    /// </summary>
    private static StockMovement CreateMovement(ReceiptLineContext context, int? batchId)
    {
        return new StockMovement
        {
            ProductId = context.ProductId,
            WarehouseId = context.WarehouseId,
            LocationId = context.LocationId,
            BatchId = batchId,
            Quantity = context.Quantity,
            ReasonCode = StockMovementReason.Receipt,
            ReferenceType = StockMovementReferenceType.PurchaseOrder,
            ReferenceId = context.GoodsReceiptLineId,
            ReferenceNumber = context.PurchaseOrderNumber,
            Notes = $"Auto-created from goods receipt {context.GoodsReceiptNumber}",
            CreatedAtUtc = context.CreatedAtUtc,
            CreatedByUserId = context.CreatedByUserId
        };
    }

    /// <summary>
    /// Upserts the stock level: increments if exists, creates if not. SDD-INV-005 Section 2.4.
    /// </summary>
    private async Task UpsertStockLevelAsync(
        ReceiptLineContext context,
        int? batchId,
        CancellationToken cancellationToken)
    {
        StockLevel? stockLevel = await _context.StockLevels
            .FirstOrDefaultAsync(s =>
                s.ProductId == context.ProductId &&
                s.WarehouseId == context.WarehouseId &&
                s.LocationId == context.LocationId &&
                s.BatchId == batchId,
                cancellationToken)
            .ConfigureAwait(false);

        if (stockLevel is null)
        {
            StockLevel newLevel = new()
            {
                ProductId = context.ProductId,
                WarehouseId = context.WarehouseId,
                LocationId = context.LocationId,
                BatchId = batchId,
                QuantityOnHand = context.Quantity,
                QuantityReserved = 0
            };
            _context.StockLevels.Add(newLevel);
        }
        else
        {
            stockLevel.QuantityOnHand += context.Quantity;
            stockLevel.ModifiedAtUtc = DateTime.UtcNow;
        }
    }
}
