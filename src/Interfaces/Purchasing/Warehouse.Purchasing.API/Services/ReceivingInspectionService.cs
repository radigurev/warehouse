using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
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
        GoodsReceiptLine? line = await Context.GoodsReceiptLines.Include(l => l.GoodsReceipt)
            .FirstOrDefaultAsync(l => l.Id == lineId && l.GoodsReceiptId == receiptId, cancellationToken).ConfigureAwait(false);
        if (line is null) return Result<GoodsReceiptLineDto>.Failure("RECEIPT_LINE_NOT_FOUND", "Goods receipt line not found.", 404);

        if (line.InspectionStatus != nameof(InspectionStatus.Pending))
            return Result<GoodsReceiptLineDto>.Failure("LINE_ALREADY_INSPECTED", "This goods receipt line has already been inspected.", 409);

        line.InspectionStatus = request.InspectionStatus;
        line.InspectionNote = request.InspectionNote;
        line.InspectedAtUtc = DateTime.UtcNow;
        line.InspectedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("InspectionCompleted", "GoodsReceiptLine", lineId, userId, null, cancellationToken).ConfigureAwait(false);

        return MapToResult<GoodsReceiptLine, GoodsReceiptLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<Result<GoodsReceiptLineDto>> ResolveQuarantineAsync(int receiptId, int lineId, ResolveQuarantineRequest request, int userId, CancellationToken cancellationToken)
    {
        GoodsReceiptLine? line = await Context.GoodsReceiptLines
            .Include(l => l.GoodsReceipt)
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
                    AcceptedByUserId = userId, AcceptedAtUtc = DateTime.UtcNow
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception) { }
        }

        return MapToResult<GoodsReceiptLine, GoodsReceiptLineDto>(line);
    }
}
