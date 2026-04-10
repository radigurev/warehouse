using MassTransit;
using Microsoft.Extensions.Logging;
using Warehouse.Inventory.API.Interfaces;
using Warehouse.ServiceModel.Events;

namespace Warehouse.Inventory.API.Consumers;

/// <summary>
/// Consumes <see cref="GoodsReceiptCompletedEvent"/> to create batches, stock movements,
/// and stock levels for each accepted receipt line. Idempotent and fault-tolerant.
/// <para>Specification: SDD-INV-005, Section 2.1.1.</para>
/// </summary>
public sealed class GoodsReceiptCompletedConsumer : IConsumer<GoodsReceiptCompletedEvent>
{
    private readonly IReceiptStockIntakeService _intakeService;
    private readonly ILogger<GoodsReceiptCompletedConsumer> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public GoodsReceiptCompletedConsumer(
        IReceiptStockIntakeService intakeService,
        ILogger<GoodsReceiptCompletedConsumer> logger)
    {
        _intakeService = intakeService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GoodsReceiptCompletedEvent> context)
    {
        GoodsReceiptCompletedEvent message = context.Message;

        try
        {
            if (!ValidateEvent(message))
                return;

            if (message.Lines.Count == 0)
            {
                _logger.LogWarning(
                    "GoodsReceiptCompletedEvent has empty Lines collection: GoodsReceiptId={GoodsReceiptId}",
                    message.GoodsReceiptId);
                return;
            }

            int succeeded = 0;
            int skipped = 0;
            int failed = 0;

            foreach (GoodsReceiptCompletedLine line in message.Lines)
            {
                try
                {
                    ReceiptLineContext lineContext = new()
                    {
                        GoodsReceiptLineId = line.GoodsReceiptLineId,
                        ProductId = line.ProductId,
                        WarehouseId = message.WarehouseId,
                        LocationId = message.LocationId,
                        Quantity = line.Quantity,
                        BatchNumber = line.BatchNumber,
                        ManufacturingDate = line.ManufacturingDate,
                        ExpiryDate = line.ExpiryDate,
                        PurchaseOrderNumber = message.PurchaseOrderNumber,
                        GoodsReceiptNumber = message.GoodsReceiptNumber,
                        CreatedByUserId = message.ReceivedByUserId,
                        CreatedAtUtc = message.ReceivedAtUtc
                    };

                    bool processed = await _intakeService.ProcessLineAsync(lineContext, context.CancellationToken).ConfigureAwait(false);
                    if (processed)
                        succeeded++;
                    else
                        skipped++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex,
                        "Failed to process receipt line: GoodsReceiptId={GoodsReceiptId}, GoodsReceiptLineId={GoodsReceiptLineId}, ProductId={ProductId}",
                        message.GoodsReceiptId, line.GoodsReceiptLineId, line.ProductId);
                }
            }

            _logger.LogInformation(
                "GoodsReceiptCompletedEvent processed: GoodsReceiptId={GoodsReceiptId}, Succeeded={Succeeded}, Skipped={Skipped}, Failed={Failed}",
                message.GoodsReceiptId, succeeded, skipped, failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception in GoodsReceiptCompletedConsumer: GoodsReceiptId={GoodsReceiptId}",
                message.GoodsReceiptId);
        }
    }

    /// <summary>
    /// Validates event-level fields per SDD-INV-005 Section 3.1.
    /// </summary>
    private bool ValidateEvent(GoodsReceiptCompletedEvent message)
    {
        if (message.GoodsReceiptId <= 0)
        {
            _logger.LogError("Invalid GoodsReceiptId: {GoodsReceiptId}", message.GoodsReceiptId);
            return false;
        }

        if (message.WarehouseId <= 0)
        {
            _logger.LogError(
                "Invalid WarehouseId: {WarehouseId}, GoodsReceiptId={GoodsReceiptId}",
                message.WarehouseId, message.GoodsReceiptId);
            return false;
        }

        if (message.ReceivedByUserId <= 0)
        {
            _logger.LogError(
                "Invalid ReceivedByUserId: {ReceivedByUserId}, GoodsReceiptId={GoodsReceiptId}",
                message.ReceivedByUserId, message.GoodsReceiptId);
            return false;
        }

        if (message.Lines is null)
        {
            _logger.LogError(
                "Lines collection is null: GoodsReceiptId={GoodsReceiptId}",
                message.GoodsReceiptId);
            return false;
        }

        return true;
    }
}
