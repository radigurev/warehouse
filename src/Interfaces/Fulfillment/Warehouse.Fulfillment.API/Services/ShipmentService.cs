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
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Services;

/// <summary>
/// Implements shipment operations: dispatch, status updates, tracking, search.
/// <para>See <see cref="IShipmentService"/>.</para>
/// </summary>
public sealed class ShipmentService : BaseFulfillmentEntityService, IShipmentService
{
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly HashSet<string> TerminalStatuses = [nameof(ShipmentStatus.Delivered), nameof(ShipmentStatus.Returned)];
    private static readonly Dictionary<string, HashSet<string>> ValidTransitions = new()
    {
        [nameof(ShipmentStatus.Dispatched)] = [nameof(ShipmentStatus.InTransit), nameof(ShipmentStatus.Delivered), nameof(ShipmentStatus.Failed)],
        [nameof(ShipmentStatus.InTransit)] = [nameof(ShipmentStatus.OutForDelivery), nameof(ShipmentStatus.Delivered), nameof(ShipmentStatus.Failed)],
        [nameof(ShipmentStatus.OutForDelivery)] = [nameof(ShipmentStatus.Delivered), nameof(ShipmentStatus.Failed)],
        [nameof(ShipmentStatus.Failed)] = [nameof(ShipmentStatus.Returned)]
    };

    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IFulfillmentEventService _eventService;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public ShipmentService(FulfillmentDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint, IFulfillmentEventService eventService)
        : base(context, mapper)
    {
        _publishEndpoint = publishEndpoint;
        _eventService = eventService;
    }

    /// <inheritdoc />
    public async Task<Result<ShipmentDetailDto>> CreateAsync(CreateShipmentRequest request, int userId, CancellationToken cancellationToken)
    {
        SalesOrder? so = await Context.SalesOrders.Include(s => s.Lines).Include(s => s.Parcels).ThenInclude(p => p.Items).FirstOrDefaultAsync(s => s.Id == request.SalesOrderId, cancellationToken).ConfigureAwait(false);
        if (so is null) return Result<ShipmentDetailDto>.Failure("SO_NOT_FOUND", "Sales order not found.", 404);
        if (so.Status != nameof(SalesOrderStatus.Packed)) return Result<ShipmentDetailDto>.Failure("SO_NOT_DISPATCHABLE", "Sales order is not in a dispatchable status (must be Packed).", 409);

        bool alreadyShipped = await Context.Shipments.AnyAsync(s => s.SalesOrderId == request.SalesOrderId, cancellationToken).ConfigureAwait(false);
        if (alreadyShipped) return Result<ShipmentDetailDto>.Failure("SO_ALREADY_SHIPPED", "Sales order has already been shipped.", 409);

        bool hasEmptyParcel = so.Parcels.Any(p => !p.Items.Any());
        if (hasEmptyParcel) return Result<ShipmentDetailDto>.Failure("EMPTY_PARCEL", "Cannot dispatch -- one or more parcels have no packed items.", 409);

        string shipmentNumber = await GenerateShipmentNumberAsync(cancellationToken).ConfigureAwait(false);
        DateTime now = DateTime.UtcNow;

        Shipment shipment = new()
        {
            ShipmentNumber = shipmentNumber, SalesOrderId = so.Id,
            CarrierId = request.CarrierId ?? so.CarrierId, CarrierServiceLevelId = request.CarrierServiceLevelId ?? so.CarrierServiceLevelId,
            Status = nameof(ShipmentStatus.Dispatched),
            ShippingStreetLine1 = so.ShippingStreetLine1, ShippingStreetLine2 = so.ShippingStreetLine2,
            ShippingCity = so.ShippingCity, ShippingStateProvince = so.ShippingStateProvince,
            ShippingPostalCode = so.ShippingPostalCode, ShippingCountryCode = so.ShippingCountryCode,
            Notes = request.Notes, DispatchedAtUtc = now, DispatchedByUserId = userId
        };

        foreach (SalesOrderLine soLine in so.Lines)
        {
            ShipmentLine shipLine = new() { SalesOrderLineId = soLine.Id, ProductId = soLine.ProductId, Quantity = soLine.PackedQuantity };
            shipment.Lines.Add(shipLine);
            soLine.ShippedQuantity = soLine.PackedQuantity;
        }

        ShipmentTracking initialTracking = new() { Status = nameof(ShipmentStatus.Dispatched), OccurredAtUtc = now, RecordedByUserId = userId };
        shipment.TrackingEntries.Add(initialTracking);

        Context.Shipments.Add(shipment);
        so.Status = nameof(SalesOrderStatus.Shipped); so.ShippedAtUtc = now; so.ModifiedAtUtc = now; so.ModifiedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await PublishShipmentDispatchedEventAsync(shipment, so, userId, cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("ShipmentDispatched", "Shipment", shipment.Id, userId, null, cancellationToken).ConfigureAwait(false);

        return await GetByIdAsync(shipment.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<ShipmentDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Shipment? shipment = await GetShipmentWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (shipment is null) return Result<ShipmentDetailDto>.Failure("SHIPMENT_NOT_FOUND", "Shipment not found.", 404);
        ShipmentDetailDto dto = Mapper.Map<ShipmentDetailDto>(shipment);
        return Result<ShipmentDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<ShipmentDto>>> SearchAsync(SearchShipmentsRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Shipment> query = BuildSearchQuery(request);
        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        IQueryable<Shipment> sorted = request.SortDescending ? query.OrderByDescending(s => s.DispatchedAtUtc) : query.OrderBy(s => s.DispatchedAtUtc);
        List<Shipment> items = await sorted.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<ShipmentDto> dtos = Mapper.Map<IReadOnlyList<ShipmentDto>>(items);
        PaginatedResponse<ShipmentDto> response = new() { Items = dtos, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
        return Result<PaginatedResponse<ShipmentDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<ShipmentDetailDto>> UpdateStatusAsync(int id, UpdateShipmentStatusRequest request, int userId, CancellationToken cancellationToken)
    {
        Shipment? shipment = await GetShipmentWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (shipment is null) return Result<ShipmentDetailDto>.Failure("SHIPMENT_NOT_FOUND", "Shipment not found.", 404);

        if (!IsValidTransition(shipment.Status, request.Status))
            return Result<ShipmentDetailDto>.Failure("INVALID_SHIPMENT_STATUS_TRANSITION", $"Cannot transition shipment from {shipment.Status} to {request.Status}.", 409);

        shipment.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.TrackingNumber)) shipment.TrackingNumber = request.TrackingNumber;
        if (!string.IsNullOrWhiteSpace(request.TrackingUrl)) shipment.TrackingUrl = request.TrackingUrl;

        ShipmentTracking tracking = new() { ShipmentId = id, Status = request.Status, Notes = request.Notes, OccurredAtUtc = DateTime.UtcNow, RecordedByUserId = userId };
        Context.ShipmentTrackingEntries.Add(tracking);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("ShipmentStatusUpdated", "Shipment", id, userId, null, cancellationToken).ConfigureAwait(false);

        ShipmentDetailDto dto = Mapper.Map<ShipmentDetailDto>(shipment);
        return Result<ShipmentDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ShipmentTrackingDto>>> GetTrackingHistoryAsync(int id, CancellationToken cancellationToken)
    {
        bool exists = await Context.Shipments.AnyAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);
        if (!exists) return Result<IReadOnlyList<ShipmentTrackingDto>>.Failure("SHIPMENT_NOT_FOUND", "Shipment not found.", 404);

        List<ShipmentTracking> entries = await Context.ShipmentTrackingEntries.Where(t => t.ShipmentId == id).OrderBy(t => t.OccurredAtUtc).AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<ShipmentTrackingDto> dtos = Mapper.Map<IReadOnlyList<ShipmentTrackingDto>>(entries);
        return Result<IReadOnlyList<ShipmentTrackingDto>>.Success(dtos);
    }

    private static bool IsValidTransition(string currentStatus, string newStatus)
    {
        if (TerminalStatuses.Contains(currentStatus)) return false;
        return ValidTransitions.TryGetValue(currentStatus, out HashSet<string>? allowed) && allowed.Contains(newStatus);
    }

    private async Task PublishShipmentDispatchedEventAsync(Shipment shipment, SalesOrder so, int userId, CancellationToken cancellationToken)
    {
        try
        {
            await _publishEndpoint.Publish(new ShipmentDispatchedEvent
            {
                ShipmentId = shipment.Id, ShipmentNumber = shipment.ShipmentNumber,
                SalesOrderId = so.Id, SalesOrderNumber = so.OrderNumber,
                WarehouseId = so.WarehouseId, DispatchedByUserId = userId, DispatchedAtUtc = shipment.DispatchedAtUtc,
                Lines = shipment.Lines.Select(l => new ShipmentDispatchedLine { ShipmentLineId = l.Id, ProductId = l.ProductId, Quantity = l.Quantity, LocationId = l.LocationId, BatchId = l.BatchId }).ToList()
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) { Logger.Warn(ex, "Failed to publish {EventType} event", "ShipmentDispatched"); }
    }

    private async Task<Shipment?> GetShipmentWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.Shipments.Include(s => s.Lines).Include(s => s.TrackingEntries).FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GenerateShipmentNumberAsync(CancellationToken cancellationToken)
    {
        string datePrefix = $"SH-{DateTime.UtcNow:yyyyMMdd}-";
        int count = await Context.Shipments.CountAsync(s => s.ShipmentNumber.StartsWith(datePrefix), cancellationToken).ConfigureAwait(false);
        return $"{datePrefix}{(count + 1):D4}";
    }

    private IQueryable<Shipment> BuildSearchQuery(SearchShipmentsRequest request)
    {
        IQueryable<Shipment> query = Context.Shipments.AsNoTracking();
        if (request.SalesOrderId.HasValue) query = query.Where(s => s.SalesOrderId == request.SalesOrderId.Value);
        if (!string.IsNullOrWhiteSpace(request.SalesOrderNumber)) query = query.Where(s => s.SalesOrder.OrderNumber.StartsWith(request.SalesOrderNumber));
        if (!string.IsNullOrWhiteSpace(request.ShipmentNumber)) query = query.Where(s => s.ShipmentNumber.StartsWith(request.ShipmentNumber));
        if (request.CarrierId.HasValue) query = query.Where(s => s.CarrierId == request.CarrierId.Value);
        if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(s => s.Status == request.Status);
        if (request.DateFrom.HasValue) query = query.Where(s => s.DispatchedAtUtc >= request.DateFrom.Value);
        if (request.DateTo.HasValue) query = query.Where(s => s.DispatchedAtUtc <= request.DateTo.Value);
        return query;
    }
}
