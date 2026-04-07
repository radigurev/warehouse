using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Interfaces;

/// <summary>
/// Defines fulfillment event history operations: record and search.
/// <para>See <see cref="FulfillmentEventDto"/>.</para>
/// </summary>
public interface IFulfillmentEventService
{
    /// <summary>
    /// Records an immutable fulfillment event.
    /// </summary>
    Task RecordEventAsync(string eventType, string entityType, int entityId, int userId, string? payload, CancellationToken cancellationToken);

    /// <summary>
    /// Searches fulfillment events with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<FulfillmentEventDto>>> SearchAsync(SearchFulfillmentEventsRequest request, CancellationToken cancellationToken);
}
