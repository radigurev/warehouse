using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NLog;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.Purchasing.API.Services.Base;
using Warehouse.Purchasing.DBModel;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Services;

/// <summary>
/// Implements receiving inspection operations: inspect and resolve quarantine.
/// <para>See <see cref="IReceivingInspectionService"/>.</para>
/// </summary>
public sealed class ReceivingInspectionService : BasePurchasingEntityService, IReceivingInspectionService
{
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IPurchaseEventService _eventService;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public ReceivingInspectionService(PurchasingDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint, IPurchaseEventService eventService)
        : base(context, mapper)
    {
        _publishEndpoint = publishEndpoint;
        _eventService = eventService;
    }

    /// <inheritdoc />
    public async Task<Result<GoodsReceiptLineDto>> InspectAsync(int receiptId, int lineId, InspectLineRequest request, int userId, CancellationToken cancellationToken)
    {
        GoodsReceiptLine? line = await Context.GoodsReceiptLines
            .Include(l => l.GoodsReceipt)
            .FirstOrDefaultAsync(l => l.Id == lineId && l.GoodsReceiptId == receiptId, cancellationToken).ConfigureAwait(false);
        if (line is null) return Result<GoodsReceiptLineDto>.Failure("RECEIPT_LINE_NOT_FOUND", "Goods receipt line not found.", 404);

        if (line.InspectionStatus != nameof(InspectionStatus.Pending))
            return Result<GoodsReceiptLineDto>.Failure("LINE_ALREADY_INSPECTED", "This goods receipt line has already been inspected.", 409);

        line.InspectionStatus = request.InspectionStatus;
        line.InspectionNote = request.InspectionNote;
        line.InspectedAtUtc = DateTime.UtcNow;
        line.InspectedByUserId = userId;

        if (request.InspectionStatus == nameof(InspectionStatus.Rejected))
            await RecalculatePOAfterRejectionAsync(line.PurchaseOrderLineId, cancellationToken).ConfigureAwait(false);

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("InspectionCompleted", "GoodsReceiptLine", lineId, userId, null, cancellationToken).ConfigureAwait(false);

        return MapToResult<GoodsReceiptLine, GoodsReceiptLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<Result<GoodsReceiptLineDto>> ResolveQuarantineAsync(int receiptId, int lineId, ResolveQuarantineRequest request, int userId, CancellationToken cancellationToken)
    {
        GoodsReceiptLine? line = await Context.GoodsReceiptLines
            .Include(l => l.GoodsReceipt)
                .ThenInclude(gr => gr.PurchaseOrder)
            .Include(l => l.PurchaseOrderLine)
            .FirstOrDefaultAsync(l => l.Id == lineId && l.GoodsReceiptId == receiptId, cancellationToken).ConfigureAwait(false);
        if (line is null) return Result<GoodsReceiptLineDto>.Failure("RECEIPT_LINE_NOT_FOUND", "Goods receipt line not found.", 404);

        if (line.InspectionStatus != nameof(InspectionStatus.Quarantined))
            return Result<GoodsReceiptLineDto>.Failure("LINE_NOT_QUARANTINED", "This goods receipt line is not quarantined.", 409);

        line.InspectionStatus = request.Resolution;
        line.InspectionNote = request.Note;
        line.InspectedAtUtc = DateTime.UtcNow;
        line.InspectedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (request.Resolution == nameof(InspectionStatus.Accepted))
        {
            try
            {
                await _publishEndpoint.Publish(new GoodsReceiptLineAcceptedEvent
                {
                    GoodsReceiptId = receiptId, GoodsReceiptLineId = lineId,
                    ProductId = line.PurchaseOrderLine?.ProductId ?? 0,
                    WarehouseId = line.GoodsReceipt.WarehouseId, LocationId = line.GoodsReceipt.LocationId,
                    Quantity = line.ReceivedQuantity, BatchNumber = line.BatchNumber,
                    ManufacturingDate = line.ManufacturingDate, ExpiryDate = line.ExpiryDate,
                    PurchaseOrderNumber = line.GoodsReceipt.PurchaseOrder.OrderNumber,
                    GoodsReceiptNumber = line.GoodsReceipt.ReceiptNumber,
                    AcceptedByUserId = userId, AcceptedAtUtc = DateTime.UtcNow
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) { Logger.Warn(ex, "Failed to publish {EventType} event", "GoodsReceiptLineAccepted"); }
        }

        return MapToResult<GoodsReceiptLine, GoodsReceiptLineDto>(line);
    }

    /// <summary>
    /// Recalculates the PO line received quantity and PO status after a line is rejected.
    /// </summary>
    private async Task RecalculatePOAfterRejectionAsync(int purchaseOrderLineId, CancellationToken cancellationToken)
    {
        PurchaseOrderLine? poLine = await Context.PurchaseOrderLines
            .Include(l => l.PurchaseOrder)
                .ThenInclude(po => po.Lines)
            .FirstOrDefaultAsync(l => l.Id == purchaseOrderLineId, cancellationToken)
            .ConfigureAwait(false);
        if (poLine is null) return;

        decimal acceptedTotal = await Context.GoodsReceiptLines
            .Where(grl => grl.PurchaseOrderLineId == purchaseOrderLineId && grl.InspectionStatus == nameof(InspectionStatus.Accepted))
            .SumAsync(grl => grl.ReceivedQuantity, cancellationToken)
            .ConfigureAwait(false);

        poLine.ReceivedQuantity = acceptedTotal;

        PurchaseOrder po = poLine.PurchaseOrder;
        RecalculatePOStatus(po);
    }

    /// <summary>
    /// Recalculates the purchase order status based on received quantities across all lines.
    /// </summary>
    private static void RecalculatePOStatus(PurchaseOrder po)
    {
        bool allReceived = po.Lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity);
        bool anyReceived = po.Lines.Any(l => l.ReceivedQuantity > 0);

        if (allReceived)
            po.Status = nameof(PurchaseOrderStatus.Received);
        else if (anyReceived)
            po.Status = nameof(PurchaseOrderStatus.PartiallyReceived);
    }
}
