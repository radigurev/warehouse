using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Interfaces;

/// <summary>
/// Defines purchase event history operations: record and search.
/// <para>See <see cref="PurchaseEventDto"/>.</para>
/// </summary>
public interface IPurchaseEventService
{
    /// <summary>
    /// Records an immutable purchase event.
    /// </summary>
    Task RecordEventAsync(string eventType, string entityType, int entityId, int userId, string? payload, CancellationToken cancellationToken);

    /// <summary>
    /// Searches purchase events with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<PurchaseEventDto>>> SearchAsync(SearchPurchaseEventsRequest request, CancellationToken cancellationToken);
}
