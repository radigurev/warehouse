using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Fulfillment.API.Services.Base;
using Warehouse.Fulfillment.DBModel;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Services;

/// <summary>
/// Implements fulfillment event history operations: record and search.
/// <para>See <see cref="IFulfillmentEventService"/>.</para>
/// </summary>
public sealed class FulfillmentEventService : BaseFulfillmentEntityService, IFulfillmentEventService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public FulfillmentEventService(FulfillmentDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task RecordEventAsync(string eventType, string entityType, int entityId, int userId, string? payload, CancellationToken cancellationToken)
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
