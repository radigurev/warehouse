using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.EventLog.API.Services.Interfaces;
using Warehouse.EventLog.DBModel;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.ServiceModel.DTOs.EventLog;
using Warehouse.ServiceModel.Requests.EventLog;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.EventLog.API.Services;

/// <summary>
/// Implements read-only query operations for centralized operations events.
/// </summary>
public sealed class EventQueryService : IEventQueryService
{
    private readonly EventLogDbContext _context;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public EventQueryService(EventLogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<OperationsEventDto>>> SearchAsync(
        SearchEventsRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<OperationsEvent> query = BuildSearchQuery(request);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        query = ApplySorting(query, request.SortBy, request.SortDirection);

        List<OperationsEvent> items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<OperationsEventDto> dtos = MapToDtos(items, request.Domain);

        PaginatedResponse<OperationsEventDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<OperationsEventDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<OperationsEventDto>> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        OperationsEvent? entity = await _context.OperationsEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return Result<OperationsEventDto>.Failure("EVENT_NOT_FOUND", "Operations event not found.", 404);
        }

        OperationsEventDto dto = MapSingleToDto(entity, includePayload: true);
        return Result<OperationsEventDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> GetEventTypesAsync(string? domain, CancellationToken cancellationToken)
    {
        IQueryable<OperationsEvent> query = _context.OperationsEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(domain))
        {
            query = query.Where(e => e.Domain == domain);
        }

        List<string> types = await query
            .Select(e => e.EventType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<string>>.Success(types);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> GetEntityTypesAsync(string? domain, CancellationToken cancellationToken)
    {
        IQueryable<OperationsEvent> query = _context.OperationsEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(domain))
        {
            query = query.Where(e => e.Domain == domain);
        }

        List<string> types = await query
            .Select(e => e.EntityType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<string>>.Success(types);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OperationsEventDto>>> GetCorrelationTimelineAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        List<OperationsEvent> events = await _context.OperationsEvents
            .AsNoTracking()
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.OccurredAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<OperationsEventDto> dtos = events
            .Select(e => MapSingleToDto(e, includePayload: false))
            .ToList();

        return Result<IReadOnlyList<OperationsEventDto>>.Success(dtos);
    }

    /// <summary>
    /// Builds the search query with optional filters.
    /// </summary>
    private IQueryable<OperationsEvent> BuildSearchQuery(SearchEventsRequest request)
    {
        IQueryable<OperationsEvent> query = _context.OperationsEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Domain))
        {
            query = query.Where(e => e.Domain == request.Domain);
        }

        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            query = query.Where(e => e.EventType == request.EventType);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query = query.Where(e => e.EntityType == request.EntityType);
        }

        if (request.EntityId.HasValue)
        {
            query = query.Where(e => e.EntityId == request.EntityId.Value);
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(e => e.UserId == request.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            query = query.Where(e => e.CorrelationId == request.CorrelationId);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(e => e.OccurredAtUtc >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(e => e.OccurredAtUtc <= request.DateTo.Value);
        }

        return query;
    }

    /// <summary>
    /// Applies sorting to the query based on the request parameters.
    /// </summary>
    private static IQueryable<OperationsEvent> ApplySorting(
        IQueryable<OperationsEvent> query,
        string? sortBy,
        string? sortDirection)
    {
        bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                          || string.IsNullOrWhiteSpace(sortDirection);

        return (sortBy?.ToLowerInvariant()) switch
        {
            "receivedat" or "receivedatutc" => descending
                ? query.OrderByDescending(e => e.ReceivedAtUtc)
                : query.OrderBy(e => e.ReceivedAtUtc),
            "eventtype" => descending
                ? query.OrderByDescending(e => e.EventType)
                : query.OrderBy(e => e.EventType),
            "entitytype" => descending
                ? query.OrderByDescending(e => e.EntityType)
                : query.OrderBy(e => e.EntityType),
            "domain" => descending
                ? query.OrderByDescending(e => e.Domain)
                : query.OrderBy(e => e.Domain),
            _ => descending
                ? query.OrderByDescending(e => e.OccurredAtUtc)
                : query.OrderBy(e => e.OccurredAtUtc),
        };
    }

    /// <summary>
    /// Maps a list of entities to DTOs, using domain-specific DTOs when a domain filter is active.
    /// </summary>
    private IReadOnlyList<OperationsEventDto> MapToDtos(List<OperationsEvent> entities, string? domainFilter)
    {
        if (string.IsNullOrWhiteSpace(domainFilter))
        {
            return _mapper.Map<IReadOnlyList<OperationsEventDto>>(entities);
        }

        return entities.Select(e => MapSingleToDto(e, includePayload: false)).ToList();
    }

    /// <summary>
    /// Maps a single entity to the appropriate domain-specific DTO.
    /// </summary>
    private OperationsEventDto MapSingleToDto(OperationsEvent entity, bool includePayload)
    {
        OperationsEventDto dto = entity switch
        {
            AuthEvent auth => _mapper.Map<AuthEventDto>(auth),
            PurchaseEvent purchase => _mapper.Map<PurchaseEventDto>(purchase),
            FulfillmentEvent fulfillment => _mapper.Map<FulfillmentEventDto>(fulfillment),
            InventoryEvent inventory => _mapper.Map<InventoryEventDto>(inventory),
            CustomerEvent customer => _mapper.Map<CustomerEventDto>(customer),
            _ => _mapper.Map<OperationsEventDto>(entity)
        };

        if (!includePayload)
        {
            dto = dto with { Payload = null };
        }

        return dto;
    }
}
