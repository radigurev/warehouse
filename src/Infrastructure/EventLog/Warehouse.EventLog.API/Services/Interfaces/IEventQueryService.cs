using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.EventLog;
using Warehouse.ServiceModel.Requests.EventLog;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.EventLog.API.Services.Interfaces;

/// <summary>
/// Defines read-only query operations for centralized operations events.
/// </summary>
public interface IEventQueryService
{
    /// <summary>
    /// Searches operations events with optional filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<OperationsEventDto>>> SearchAsync(
        SearchEventsRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets a single operations event by ID with full payload.
    /// </summary>
    Task<Result<OperationsEventDto>> GetByIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets distinct event types, optionally filtered by domain.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> GetEventTypesAsync(string? domain, CancellationToken cancellationToken);

    /// <summary>
    /// Gets distinct entity types, optionally filtered by domain.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> GetEntityTypesAsync(string? domain, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all events sharing a correlation ID, sorted chronologically.
    /// </summary>
    Task<Result<IReadOnlyList<OperationsEventDto>>> GetCorrelationTimelineAsync(
        string correlationId,
        CancellationToken cancellationToken);
}
