using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
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
/// Implements purchase event history operations: record and search.
/// <para>See <see cref="IPurchaseEventService"/>.</para>
/// </summary>
public sealed class PurchaseEventService : BasePurchasingEntityService, IPurchaseEventService
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PurchaseEventService> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public PurchaseEventService(
        PurchasingDbContext context,
        IMapper mapper,
        IPublishEndpoint publishEndpoint,
        ILogger<PurchaseEventService> logger)
        : base(context, mapper)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RecordEventAsync(
        string eventType,
        string entityType,
        int entityId,
        int userId,
        string? payload,
        CancellationToken cancellationToken,
        string? supplierName = null,
        string? documentNumber = null)
    {
        PurchaseEvent purchaseEvent = new()
        {
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            OccurredAtUtc = DateTime.UtcNow,
            Payload = payload
        };

        Context.PurchaseEvents.Add(purchaseEvent);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _publishEndpoint.Publish(new PurchaseEventOccurredEvent
            {
                EventType = eventType,
                EntityType = entityType,
                EntityId = entityId,
                UserId = userId,
                OccurredAtUtc = purchaseEvent.OccurredAtUtc,
                Payload = payload,
                SupplierName = supplierName,
                DocumentNumber = documentNumber
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish PurchaseEventOccurredEvent for {EventType} {EntityType}:{EntityId}",
                eventType, entityType, entityId);
        }
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<PurchaseEventDto>>> SearchAsync(
        SearchPurchaseEventsRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<PurchaseEvent> query = BuildSearchQuery(request);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<PurchaseEvent> items = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<PurchaseEventDto> dtos = Mapper.Map<IReadOnlyList<PurchaseEventDto>>(items);

        PaginatedResponse<PurchaseEventDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<PurchaseEventDto>>.Success(response);
    }

    /// <summary>
    /// Builds the search query with optional filters.
    /// </summary>
    private IQueryable<PurchaseEvent> BuildSearchQuery(SearchPurchaseEventsRequest request)
    {
        IQueryable<PurchaseEvent> query = Context.PurchaseEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.EventType))
            query = query.Where(e => e.EventType == request.EventType);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(e => e.EntityType == request.EntityType);

        if (request.EntityId.HasValue)
            query = query.Where(e => e.EntityId == request.EntityId.Value);

        if (request.UserId.HasValue)
            query = query.Where(e => e.UserId == request.UserId.Value);

        if (request.DateFrom.HasValue)
            query = query.Where(e => e.OccurredAtUtc >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(e => e.OccurredAtUtc <= request.DateTo.Value);

        return query;
    }
}
