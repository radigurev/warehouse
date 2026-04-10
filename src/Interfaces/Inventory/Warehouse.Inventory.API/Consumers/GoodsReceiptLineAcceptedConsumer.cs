using MassTransit;
using Microsoft.Extensions.Logging;
using Warehouse.Inventory.API.Interfaces;
using Warehouse.ServiceModel.Events;

namespace Warehouse.Inventory.API.Consumers;

/// <summary>
/// Consumes <see cref="GoodsReceiptLineAcceptedEvent"/> to create batches, stock movements,
/// and stock levels for a single quarantine-resolved receipt line. Idempotent and fault-tolerant.
/// <para>Specification: SDD-INV-005, Section 2.1.2.</para>
/// </summary>
public sealed class GoodsReceiptLineAcceptedConsumer : IConsumer<GoodsReceiptLineAcceptedEvent>
{
    private readonly IReceiptStockIntakeService _intakeService;
    private readonly ILogger<GoodsReceiptLineAcceptedConsumer> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public GoodsReceiptLineAcceptedConsumer(
        IReceiptStockIntakeService intakeService,
        ILogger<GoodsReceiptLineAcceptedConsumer> logger)
    {
        _intakeService = intakeService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GoodsReceiptLineAcceptedEvent> context)
    {
        GoodsReceiptLineAcceptedEvent message = context.Message;

        try
        {
            if (message.GoodsReceiptLineId <= 0)
            {
                _logger.LogError(
                    "Invalid GoodsReceiptLineId: {GoodsReceiptLineId}, GoodsReceiptId={GoodsReceiptId}",
                    message.GoodsReceiptLineId, message.GoodsReceiptId);
                return;
            }

            if (message.WarehouseId <= 0)
            {
                _logger.LogError(
                    "Invalid WarehouseId: {WarehouseId}, GoodsReceiptLineId={GoodsReceiptLineId}",
                    message.WarehouseId, message.GoodsReceiptLineId);
                return;
            }

            ReceiptLineContext lineContext = new()
            {
                GoodsReceiptLineId = message.GoodsReceiptLineId,
                ProductId = message.ProductId,
                WarehouseId = message.WarehouseId,
                LocationId = message.LocationId,
                Quantity = message.Quantity,
                BatchNumber = message.BatchNumber,
                ManufacturingDate = message.ManufacturingDate,
                ExpiryDate = message.ExpiryDate,
                PurchaseOrderNumber = message.PurchaseOrderNumber,
                GoodsReceiptNumber = message.GoodsReceiptNumber,
                CreatedByUserId = message.AcceptedByUserId,
                CreatedAtUtc = message.AcceptedAtUtc
            };

            bool processed = await _intakeService.ProcessLineAsync(lineContext, context.CancellationToken).ConfigureAwait(false);

            if (processed)
            {
                _logger.LogInformation(
                    "GoodsReceiptLineAcceptedEvent processed: GoodsReceiptLineId={GoodsReceiptLineId}",
                    message.GoodsReceiptLineId);
            }
            else
            {
                _logger.LogInformation(
                    "GoodsReceiptLineAcceptedEvent skipped (duplicate or validation): GoodsReceiptLineId={GoodsReceiptLineId}",
                    message.GoodsReceiptLineId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception in GoodsReceiptLineAcceptedConsumer: GoodsReceiptLineId={GoodsReceiptLineId}, GoodsReceiptId={GoodsReceiptId}",
                message.GoodsReceiptLineId, message.GoodsReceiptId);
        }
    }
}
