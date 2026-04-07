using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Controllers;

/// <summary>
/// Handles purchase event history queries.
/// <para>See <see cref="IPurchaseEventService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/purchase-events")]
[Authorize]
public sealed class PurchaseEventsController : BaseApiController
{
    private readonly IPurchaseEventService _eventService;

    /// <summary>Initializes a new instance with the specified event service.</summary>
    public PurchaseEventsController(IPurchaseEventService eventService) { _eventService = eventService; }

    /// <summary>Lists purchase events with filters and pagination.</summary>
    [HttpGet]
    [RequirePermission("purchase-events:read")]
    [ProducesResponseType(typeof(PaginatedResponse<PurchaseEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchEventsAsync([FromQuery] SearchPurchaseEventsRequest request, CancellationToken cancellationToken)
    { Result<PaginatedResponse<PurchaseEventDto>> result = await _eventService.SearchAsync(request, cancellationToken); return ToActionResult(result); }
}
