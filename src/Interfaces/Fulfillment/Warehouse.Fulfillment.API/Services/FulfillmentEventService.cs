using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
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
/// Implements fulfillment event history operations: record and search.
/// <para>See <see cref="IFulfillmentEventService"/>.</para>
/// </summary>
public sealed class FulfillmentEventService : BaseFulfillmentEntityService, IFulfillmentEventService
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<FulfillmentEventService> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public FulfillmentEventService(
        FulfillmentDbContext context,
        IMapper mapper,
        IPublishEndpoint publishEndpoint,
        ILogger<FulfillmentEventService> logger)
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
        string? customerName = null,
        string? documentNumber = null)
    {
        FulfillmentEvent fulfillmentEvent = new()
        {
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            OccurredAtUtc = DateTime.UtcNow,
            Payload = payload
        };

        Context.FulfillmentEvents.Add(fulfillmentEvent);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _publishEndpoint.Publish(new FulfillmentEventOccurredEvent
            {
                EventType = eventType,
                EntityType = entityType,
                EntityId = entityId,
                UserId = userId,
                OccurredAtUtc = fulfillmentEvent.OccurredAtUtc,
                Payload = payload,
                CustomerName = customerName,
                DocumentNumber = documentNumber
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish FulfillmentEventOccurredEvent for {EventType} {EntityType}:{EntityId}",
                eventType, entityType, entityId);
        }
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<FulfillmentEventDto>>> SearchAsync(SearchFulfillmentEventsRequest request, CancellationToken cancellationToken)
    {
        IQueryable<FulfillmentEvent> query = BuildSearchQuery(request);
        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<FulfillmentEvent> items = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<FulfillmentEventDto> dtos = Mapper.Map<IReadOnlyList<FulfillmentEventDto>>(items);
        PaginatedResponse<FulfillmentEventDto> response = new() { Items = dtos, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
        return Result<PaginatedResponse<FulfillmentEventDto>>.Success(response);
    }

    /// <summary>
    /// Builds the search query with optional filters.
    /// </summary>
    private IQueryable<FulfillmentEvent> BuildSearchQuery(SearchFulfillmentEventsRequest request)
    {
        IQueryable<FulfillmentEvent> query = Context.FulfillmentEvents.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.EventType)) query = query.Where(e => e.EventType == request.EventType);
        if (!string.IsNullOrWhiteSpace(request.EntityType)) query = query.Where(e => e.EntityType == request.EntityType);
        if (request.EntityId.HasValue) query = query.Where(e => e.EntityId == request.EntityId.Value);
        if (request.UserId.HasValue) query = query.Where(e => e.UserId == request.UserId.Value);
        if (request.DateFrom.HasValue) query = query.Where(e => e.OccurredAtUtc >= request.DateFrom.Value);
        if (request.DateTo.HasValue) query = query.Where(e => e.OccurredAtUtc <= request.DateTo.Value);
        return query;
    }
}
