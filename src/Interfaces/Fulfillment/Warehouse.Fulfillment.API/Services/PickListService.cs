using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NLog;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Fulfillment.API.Services.Base;
using Warehouse.Fulfillment.DBModel;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.Infrastructure.Correlation;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Services;

/// <summary>
/// Implements pick list operations: generate, confirm pick, cancel, search.
/// <para>See <see cref="IPickListService"/>.</para>
/// </summary>
public sealed class PickListService : BaseFulfillmentEntityService, IPickListService
{
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly IFulfillmentEventService _eventService;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public PickListService(FulfillmentDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint, ICorrelationIdAccessor correlationIdAccessor, IFulfillmentEventService eventService)
        : base(context, mapper)
    {
        _publishEndpoint = publishEndpoint;
        _correlationIdAccessor = correlationIdAccessor;
        _eventService = eventService;
    }

    /// <inheritdoc />
    public async Task<Result<PickListDetailDto>> GenerateAsync(GeneratePickListRequest request, int userId, CancellationToken cancellationToken)
    {
        SalesOrder? so = await Context.SalesOrders.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == request.SalesOrderId, cancellationToken).ConfigureAwait(false);
        if (so is null) return Result<PickListDetailDto>.Failure("SO_NOT_FOUND", "Sales order not found.", 404);

        if (so.Status != nameof(SalesOrderStatus.Confirmed) && so.Status != nameof(SalesOrderStatus.Picking))
            return Result<PickListDetailDto>.Failure("SO_NOT_PICKABLE", "Sales order is not in a pickable status.", 409);

        // TODO: [SDD-FULF-001 §2.2.1] Validate sufficient available stock
        // via typed HttpClient to Inventory.API with Polly resilience.
        // Error: INSUFFICIENT_STOCK (409)

        List<SalesOrderLine> unallocatedLines = GetUnallocatedLines(so);
        if (unallocatedLines.Count == 0) return Result<PickListDetailDto>.Failure("SO_FULLY_ALLOCATED", "All sales order lines are already allocated to pick lists.", 409);

        string pickListNumber = await GeneratePickListNumberAsync(cancellationToken).ConfigureAwait(false);

        PickList pickList = new() { PickListNumber = pickListNumber, SalesOrderId = so.Id, Status = nameof(PickListStatus.Pending), CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = userId };

        foreach (SalesOrderLine soLine in unallocatedLines)
        {
            decimal remainingToAllocate = soLine.OrderedQuantity - GetAllocatedQuantity(soLine.Id);
            if (remainingToAllocate <= 0) continue;

            PickListLine plLine = new() { SalesOrderLineId = soLine.Id, ProductId = soLine.ProductId, WarehouseId = so.WarehouseId, RequestedQuantity = remainingToAllocate, Status = nameof(PickListStatus.Pending) };
            pickList.Lines.Add(plLine);
        }

        Context.PickLists.Add(pickList);

        if (so.Status == nameof(SalesOrderStatus.Confirmed))
            so.Status = nameof(SalesOrderStatus.Picking);

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("PickListGenerated", "PickList", pickList.Id, userId, null, cancellationToken).ConfigureAwait(false);
        await PublishReservationRequestedAsync(pickList, so, userId, cancellationToken).ConfigureAwait(false);

        return await GetByIdAsync(pickList.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<PickListDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        PickList? pickList = await Context.PickLists.Include(pl => pl.Lines).FirstOrDefaultAsync(pl => pl.Id == id, cancellationToken).ConfigureAwait(false);
        if (pickList is null) return Result<PickListDetailDto>.Failure("PICK_LIST_NOT_FOUND", "Pick list not found.", 404);
        PickListDetailDto dto = Mapper.Map<PickListDetailDto>(pickList);
        return Result<PickListDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<PickListDto>>> SearchAsync(SearchPickListsRequest request, CancellationToken cancellationToken)
    {
        IQueryable<PickList> query = BuildSearchQuery(request);
        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        IQueryable<PickList> sorted = request.SortDescending ? query.OrderByDescending(pl => pl.CreatedAtUtc) : query.OrderBy(pl => pl.CreatedAtUtc);
        List<PickList> items = await sorted.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<PickListDto> dtos = Mapper.Map<IReadOnlyList<PickListDto>>(items);
        PaginatedResponse<PickListDto> response = new() { Items = dtos, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
        return Result<PaginatedResponse<PickListDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<PickListLineDto>> ConfirmPickAsync(int pickListId, int lineId, ConfirmPickRequest request, int userId, CancellationToken cancellationToken)
    {
        PickList? pickList = await Context.PickLists.Include(pl => pl.Lines).FirstOrDefaultAsync(pl => pl.Id == pickListId, cancellationToken).ConfigureAwait(false);
        if (pickList is null) return Result<PickListLineDto>.Failure("PICK_LIST_NOT_FOUND", "Pick list not found.", 404);

        PickListLine? line = pickList.Lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null) return Result<PickListLineDto>.Failure("PICK_LIST_LINE_NOT_FOUND", "Pick list line not found.", 404);
        if (line.Status == nameof(PickListStatus.Completed)) return Result<PickListLineDto>.Failure("LINE_ALREADY_PICKED", "This pick list line has already been picked.", 409);
        if (request.ActualQuantity > line.RequestedQuantity) return Result<PickListLineDto>.Failure("OVER_PICK", "Picked quantity exceeds the requested quantity.", 409);

        line.ActualQuantity = request.ActualQuantity; line.Status = nameof(PickListStatus.Completed); line.PickedAtUtc = DateTime.UtcNow; line.PickedByUserId = userId;

        await UpdateSOPickedQuantityAsync(line, cancellationToken).ConfigureAwait(false);

        if (request.ActualQuantity < line.RequestedQuantity)
            await PublishReservationReleasedForShortPickAsync(pickList, line, userId, cancellationToken).ConfigureAwait(false);

        bool allPicked = pickList.Lines.All(l => l.Status == nameof(PickListStatus.Completed));
        if (allPicked) { pickList.Status = nameof(PickListStatus.Completed); pickList.CompletedAtUtc = DateTime.UtcNow; }

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        PickListLineDto dto = Mapper.Map<PickListLineDto>(line);
        return Result<PickListLineDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PickListDetailDto>> CancelAsync(int id, int userId, CancellationToken cancellationToken)
    {
        PickList? pickList = await Context.PickLists.Include(pl => pl.Lines).FirstOrDefaultAsync(pl => pl.Id == id, cancellationToken).ConfigureAwait(false);
        if (pickList is null) return Result<PickListDetailDto>.Failure("PICK_LIST_NOT_FOUND", "Pick list not found.", 404);
        if (pickList.Status == nameof(PickListStatus.Completed)) return Result<PickListDetailDto>.Failure("PICK_LIST_ALREADY_COMPLETED", "Pick list has already been completed.", 409);

        List<PickListLine> unpickedLines = pickList.Lines.Where(l => l.Status != nameof(PickListStatus.Completed)).ToList();
        pickList.Status = nameof(PickListStatus.Cancelled);

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await PublishReservationReleasedForCancelAsync(pickList, unpickedLines, userId, cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("PickListCancelled", "PickList", id, userId, null, cancellationToken).ConfigureAwait(false);

        PickListDetailDto dto = Mapper.Map<PickListDetailDto>(pickList);
        return Result<PickListDetailDto>.Success(dto);
    }

    private List<SalesOrderLine> GetUnallocatedLines(SalesOrder so)
    {
        return so.Lines.Where(l => l.OrderedQuantity > GetAllocatedQuantity(l.Id)).ToList();
    }

    private decimal GetAllocatedQuantity(int soLineId)
    {
        return Context.PickListLines
            .Where(pll => pll.SalesOrderLineId == soLineId && pll.PickList.Status != nameof(PickListStatus.Cancelled))
            .Sum(pll => pll.RequestedQuantity);
    }

    private async Task UpdateSOPickedQuantityAsync(PickListLine line, CancellationToken cancellationToken)
    {
        SalesOrderLine? soLine = await Context.SalesOrderLines.FirstOrDefaultAsync(l => l.Id == line.SalesOrderLineId, cancellationToken).ConfigureAwait(false);
        if (soLine is not null) soLine.PickedQuantity += line.ActualQuantity ?? 0;
    }

    private async Task PublishReservationRequestedAsync(PickList pickList, SalesOrder so, int userId, CancellationToken cancellationToken)
    {
        try
        {
            await _publishEndpoint.PublishWithCorrelationAsync(new StockReservationRequestedEvent
            {
                PickListId = pickList.Id, PickListNumber = pickList.PickListNumber,
                SalesOrderId = so.Id, SalesOrderNumber = so.OrderNumber,
                RequestedByUserId = userId, RequestedAtUtc = DateTime.UtcNow,
                Lines = pickList.Lines.Select(l => new StockReservationLine { PickListLineId = l.Id, ProductId = l.ProductId, WarehouseId = l.WarehouseId, LocationId = l.LocationId, Quantity = l.RequestedQuantity }).ToList()
            }, _correlationIdAccessor, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) { Logger.Warn(ex, "Failed to publish {EventType} event", "StockReservationRequested"); }
    }

    private async Task PublishReservationReleasedForShortPickAsync(PickList pickList, PickListLine line, int userId, CancellationToken cancellationToken)
    {
        decimal releasedQty = line.RequestedQuantity - (line.ActualQuantity ?? 0);
        try
        {
            await _publishEndpoint.PublishWithCorrelationAsync(new StockReservationReleasedEvent
            {
                PickListId = pickList.Id, PickListNumber = pickList.PickListNumber,
                SalesOrderId = pickList.SalesOrderId, ReleasedByUserId = userId, ReleasedAtUtc = DateTime.UtcNow,
                Lines = [new StockReservationLine { PickListLineId = line.Id, ProductId = line.ProductId, WarehouseId = line.WarehouseId, LocationId = line.LocationId, Quantity = releasedQty }]
            }, _correlationIdAccessor, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) { Logger.Warn(ex, "Failed to publish {EventType} event", "StockReservationReleased"); }
    }

    private async Task PublishReservationReleasedForCancelAsync(PickList pickList, List<PickListLine> unpickedLines, int userId, CancellationToken cancellationToken)
    {
        if (unpickedLines.Count == 0) return;
        try
        {
            await _publishEndpoint.PublishWithCorrelationAsync(new StockReservationReleasedEvent
            {
                PickListId = pickList.Id, PickListNumber = pickList.PickListNumber,
                SalesOrderId = pickList.SalesOrderId, ReleasedByUserId = userId, ReleasedAtUtc = DateTime.UtcNow,
                Lines = unpickedLines.Select(l => new StockReservationLine { PickListLineId = l.Id, ProductId = l.ProductId, WarehouseId = l.WarehouseId, LocationId = l.LocationId, Quantity = l.RequestedQuantity }).ToList()
            }, _correlationIdAccessor, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) { Logger.Warn(ex, "Failed to publish {EventType} event", "StockReservationReleased"); }
    }

    private async Task<string> GeneratePickListNumberAsync(CancellationToken cancellationToken)
    {
        string datePrefix = $"PL-{DateTime.UtcNow:yyyyMMdd}-";
        int count = await Context.PickLists.CountAsync(pl => pl.PickListNumber.StartsWith(datePrefix), cancellationToken).ConfigureAwait(false);
        return $"{datePrefix}{(count + 1):D4}";
    }

    private IQueryable<PickList> BuildSearchQuery(SearchPickListsRequest request)
    {
        IQueryable<PickList> query = Context.PickLists.AsNoTracking();
        if (request.SalesOrderId.HasValue) query = query.Where(pl => pl.SalesOrderId == request.SalesOrderId.Value);
        if (!string.IsNullOrWhiteSpace(request.SalesOrderNumber)) query = query.Where(pl => pl.SalesOrder.OrderNumber.StartsWith(request.SalesOrderNumber));
        if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(pl => pl.Status == request.Status);
        if (request.DateFrom.HasValue) query = query.Where(pl => pl.CreatedAtUtc >= request.DateFrom.Value);
        if (request.DateTo.HasValue) query = query.Where(pl => pl.CreatedAtUtc <= request.DateTo.Value);
        return query;
    }
}
