using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NLog;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Correlation;
using Warehouse.Infrastructure.Sequences;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.Purchasing.API.Services.Base;
using Warehouse.Purchasing.DBModel;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Services;

/// <summary>
/// Implements goods receipt operations: create, complete, search.
/// <para>See <see cref="IGoodsReceiptService"/>.</para>
/// </summary>
public sealed class GoodsReceiptService : BasePurchasingEntityService, IGoodsReceiptService
{
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly IPurchaseEventService _eventService;
    private readonly ISequenceGenerator _sequenceGenerator;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public GoodsReceiptService(
        PurchasingDbContext context,
        IMapper mapper,
        IPublishEndpoint publishEndpoint,
        ICorrelationIdAccessor correlationIdAccessor,
        IPurchaseEventService eventService,
        ISequenceGenerator sequenceGenerator)
        : base(context, mapper)
    {
        _publishEndpoint = publishEndpoint;
        _correlationIdAccessor = correlationIdAccessor;
        _eventService = eventService;
        _sequenceGenerator = sequenceGenerator;
    }

    /// <inheritdoc />
    public async Task<Result<GoodsReceiptDetailDto>> CreateAsync(CreateGoodsReceiptRequest request, int userId, CancellationToken cancellationToken)
    {
        PurchaseOrder? po = await Context.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId, cancellationToken).ConfigureAwait(false);
        if (po is null) return Result<GoodsReceiptDetailDto>.Failure("PO_NOT_FOUND", "Purchase order not found.", 404);

        if (po.Status != nameof(PurchaseOrderStatus.Confirmed) && po.Status != nameof(PurchaseOrderStatus.PartiallyReceived))
            return Result<GoodsReceiptDetailDto>.Failure("PO_NOT_RECEIVABLE", "Purchase order is not in a receivable status.", 409);

        string receiptNumber = await GenerateReceiptNumberAsync(cancellationToken).ConfigureAwait(false);

        GoodsReceipt receipt = new()
        {
            ReceiptNumber = receiptNumber, PurchaseOrderId = request.PurchaseOrderId, WarehouseId = request.WarehouseId,
            LocationId = request.LocationId, Status = nameof(GoodsReceiptStatus.Completed), Notes = request.Notes,
            ReceivedAtUtc = DateTime.UtcNow, CompletedAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = userId
        };

        Dictionary<int, string> productCodeMap = await ResolveProductCodesAsync(
            request.Lines.Select(l => l.PurchaseOrderLineId).ToList(),
            po.Lines,
            cancellationToken).ConfigureAwait(false);

        foreach (CreateGoodsReceiptLineRequest lineReq in request.Lines)
        {
            PurchaseOrderLine? poLine = po.Lines.FirstOrDefault(l => l.Id == lineReq.PurchaseOrderLineId);
            if (poLine is null) return Result<GoodsReceiptDetailDto>.Failure("INVALID_PO_LINE", "The specified PO line does not belong to this purchase order.", 400);

            decimal remaining = poLine.OrderedQuantity - poLine.ReceivedQuantity;
            if (remaining <= 0) return Result<GoodsReceiptDetailDto>.Failure("LINE_FULLY_RECEIVED", "This PO line has already been fully received.", 409);
            if (lineReq.ReceivedQuantity > remaining) return Result<GoodsReceiptDetailDto>.Failure("OVER_RECEIPT", "Received quantity exceeds remaining quantity on the PO line.", 409);

            string batchNumber = await GenerateBatchNumberForLineAsync(
                poLine.ProductId, productCodeMap, cancellationToken).ConfigureAwait(false);

            GoodsReceiptLine line = new()
            {
                PurchaseOrderLineId = lineReq.PurchaseOrderLineId, ReceivedQuantity = lineReq.ReceivedQuantity,
                BatchNumber = batchNumber, ManufacturingDate = lineReq.ManufacturingDate, ExpiryDate = lineReq.ExpiryDate,
                InspectionStatus = nameof(InspectionStatus.Accepted)
            };
            receipt.Lines.Add(line);
        }

        foreach (GoodsReceiptLine grLine in receipt.Lines)
        {
            PurchaseOrderLine? poLine = po.Lines.FirstOrDefault(l => l.Id == grLine.PurchaseOrderLineId);
            if (poLine is not null)
                poLine.ReceivedQuantity += grLine.ReceivedQuantity;
        }

        bool allReceived = po.Lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity);
        bool anyReceived = po.Lines.Any(l => l.ReceivedQuantity > 0);
        if (allReceived) po.Status = nameof(PurchaseOrderStatus.Received);
        else if (anyReceived) po.Status = nameof(PurchaseOrderStatus.PartiallyReceived);

        Context.GoodsReceipts.Add(receipt);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _eventService.RecordEventAsync("GoodsReceiptCompleted", "GoodsReceipt", receipt.Id, userId, null, cancellationToken).ConfigureAwait(false);
        await PublishGoodsReceiptCompletedEventAsync(receipt, userId, cancellationToken, po).ConfigureAwait(false);

        return await GetByIdAsync(receipt.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<GoodsReceiptDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        GoodsReceipt? receipt = await GetReceiptWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (receipt is null) return Result<GoodsReceiptDetailDto>.Failure("RECEIPT_NOT_FOUND", "Goods receipt not found.", 404);
        GoodsReceiptDetailDto dto = Mapper.Map<GoodsReceiptDetailDto>(receipt);
        return Result<GoodsReceiptDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<GoodsReceiptDto>>> SearchAsync(SearchGoodsReceiptsRequest request, CancellationToken cancellationToken)
    {
        IQueryable<GoodsReceipt> query = BuildSearchQuery(request);
        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        IQueryable<GoodsReceipt> sorted = request.SortDescending
            ? query.OrderByDescending(gr => gr.ReceivedAtUtc)
            : query.OrderBy(gr => gr.ReceivedAtUtc);

        List<GoodsReceipt> items = await sorted.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<GoodsReceiptDto> dtos = Mapper.Map<IReadOnlyList<GoodsReceiptDto>>(items);
        PaginatedResponse<GoodsReceiptDto> response = new() { Items = dtos, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
        return Result<PaginatedResponse<GoodsReceiptDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<GoodsReceiptDetailDto>> CompleteAsync(int id, int userId, CancellationToken cancellationToken)
    {
        GoodsReceipt? receipt = await GetReceiptWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (receipt is null) return Result<GoodsReceiptDetailDto>.Failure("RECEIPT_NOT_FOUND", "Goods receipt not found.", 404);
        if (receipt.Status == nameof(GoodsReceiptStatus.Completed)) return Result<GoodsReceiptDetailDto>.Failure("RECEIPT_ALREADY_COMPLETED", "Goods receipt is already completed.", 409);

        receipt.Status = nameof(GoodsReceiptStatus.Completed);
        receipt.CompletedAtUtc = DateTime.UtcNow;

        await UpdatePOReceivedQuantitiesAsync(receipt, cancellationToken).ConfigureAwait(false);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await PublishGoodsReceiptCompletedEventAsync(receipt, userId, cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("GoodsReceiptCompleted", "GoodsReceipt", id, userId, null, cancellationToken).ConfigureAwait(false);

        GoodsReceiptDetailDto dto = Mapper.Map<GoodsReceiptDetailDto>(receipt);
        return Result<GoodsReceiptDetailDto>.Success(dto);
    }

    private async Task UpdatePOReceivedQuantitiesAsync(GoodsReceipt receipt, CancellationToken cancellationToken)
    {
        PurchaseOrder? po = await Context.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == receipt.PurchaseOrderId, cancellationToken).ConfigureAwait(false);
        if (po is null) return;

        foreach (GoodsReceiptLine grLine in receipt.Lines.Where(l => l.InspectionStatus == nameof(InspectionStatus.Accepted) || l.InspectionStatus == nameof(InspectionStatus.Pending)))
        {
            PurchaseOrderLine? poLine = po.Lines.FirstOrDefault(l => l.Id == grLine.PurchaseOrderLineId);
            if (poLine is not null && grLine.InspectionStatus == nameof(InspectionStatus.Accepted))
                poLine.ReceivedQuantity += grLine.ReceivedQuantity;
        }

        bool allReceived = po.Lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity);
        bool anyReceived = po.Lines.Any(l => l.ReceivedQuantity > 0);

        if (allReceived) po.Status = nameof(PurchaseOrderStatus.Received);
        else if (anyReceived) po.Status = nameof(PurchaseOrderStatus.PartiallyReceived);
    }

    private async Task PublishGoodsReceiptCompletedEventAsync(
        GoodsReceipt receipt, int userId, CancellationToken cancellationToken, PurchaseOrder? po = null)
    {
        if (po is null)
            po = await Context.PurchaseOrders.Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == receipt.PurchaseOrderId, cancellationToken).ConfigureAwait(false);

        Dictionary<int, int> poLineProductMap = po?.Lines.ToDictionary(l => l.Id, l => l.ProductId) ?? [];

        List<GoodsReceiptCompletedLine> acceptedLines = receipt.Lines
            .Where(l => l.InspectionStatus == nameof(InspectionStatus.Accepted))
            .Select(l => new GoodsReceiptCompletedLine
            {
                GoodsReceiptLineId = l.Id,
                ProductId = l.PurchaseOrderLine?.ProductId ?? poLineProductMap.GetValueOrDefault(l.PurchaseOrderLineId),
                Quantity = l.ReceivedQuantity, BatchNumber = l.BatchNumber,
                ManufacturingDate = l.ManufacturingDate, ExpiryDate = l.ExpiryDate
            }).ToList();

        if (acceptedLines.Count == 0) return;

        try
        {
            await _publishEndpoint.PublishWithCorrelationAsync(new GoodsReceiptCompletedEvent
            {
                GoodsReceiptId = receipt.Id, GoodsReceiptNumber = receipt.ReceiptNumber,
                PurchaseOrderId = receipt.PurchaseOrderId, PurchaseOrderNumber = po?.OrderNumber ?? string.Empty,
                WarehouseId = receipt.WarehouseId, LocationId = receipt.LocationId,
                ReceivedByUserId = userId, ReceivedAtUtc = receipt.ReceivedAtUtc, Lines = acceptedLines
            }, _correlationIdAccessor, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) { Logger.Warn(ex, "Failed to publish {EventType} event", "GoodsReceiptCompleted"); }
    }

    private async Task<GoodsReceipt?> GetReceiptWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.GoodsReceipts.Include(gr => gr.PurchaseOrder).Include(gr => gr.Lines).ThenInclude(l => l.PurchaseOrderLine).FirstOrDefaultAsync(gr => gr.Id == id, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GenerateReceiptNumberAsync(CancellationToken cancellationToken)
    {
        string datePrefix = $"GR-{DateTime.UtcNow:yyyyMMdd}-";
        int count = await Context.GoodsReceipts.CountAsync(gr => gr.ReceiptNumber.StartsWith(datePrefix), cancellationToken).ConfigureAwait(false);
        return $"{datePrefix}{(count + 1):D4}";
    }

    /// <summary>
    /// Resolves product codes for PO lines via the ProductLookup view.
    /// </summary>
    private async Task<Dictionary<int, string>> ResolveProductCodesAsync(
        List<int> poLineIds,
        ICollection<PurchaseOrderLine> poLines,
        CancellationToken cancellationToken)
    {
        List<int> productIds = poLines
            .Where(l => poLineIds.Contains(l.Id))
            .Select(l => l.ProductId)
            .Distinct()
            .ToList();

        if (productIds.Count == 0) return [];

        return await Context.ProductLookups
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Code, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Generates a batch number for a receipt line using the sequence generator.
    /// </summary>
    private async Task<string> GenerateBatchNumberForLineAsync(
        int productId,
        Dictionary<int, string> productCodeMap,
        CancellationToken cancellationToken)
    {
        string productCode = productCodeMap.GetValueOrDefault(productId, $"PROD-{productId}");
        return await _sequenceGenerator
            .NextBatchNumberAsync(productCode, cancellationToken)
            .ConfigureAwait(false);
    }

    private IQueryable<GoodsReceipt> BuildSearchQuery(SearchGoodsReceiptsRequest request)
    {
        IQueryable<GoodsReceipt> query = Context.GoodsReceipts.AsNoTracking().Include(gr => gr.PurchaseOrder);
        if (request.PurchaseOrderId.HasValue) query = query.Where(gr => gr.PurchaseOrderId == request.PurchaseOrderId.Value);
        if (!string.IsNullOrWhiteSpace(request.PurchaseOrderNumber)) query = query.Where(gr => gr.PurchaseOrder.OrderNumber.StartsWith(request.PurchaseOrderNumber));
        if (!string.IsNullOrWhiteSpace(request.ReceiptNumber)) query = query.Where(gr => gr.ReceiptNumber.StartsWith(request.ReceiptNumber));
        if (request.WarehouseId.HasValue) query = query.Where(gr => gr.WarehouseId == request.WarehouseId.Value);
        if (request.DateFrom.HasValue) query = query.Where(gr => gr.ReceivedAtUtc >= request.DateFrom.Value);
        if (request.DateTo.HasValue) query = query.Where(gr => gr.ReceivedAtUtc <= request.DateTo.Value);
        return query;
    }
}
