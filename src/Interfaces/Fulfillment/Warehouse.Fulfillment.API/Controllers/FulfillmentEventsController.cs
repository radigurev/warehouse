using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Controllers;

/// <summary>
/// Handles fulfillment event history queries.
/// <para>See <see cref="IFulfillmentEventService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/fulfillment-events")]
[Authorize]
public sealed class FulfillmentEventsController : BaseApiController
{
    private readonly IFulfillmentEventService _eventService;

    /// <summary>Initializes a new instance with the specified event service.</summary>
    public FulfillmentEventsController(IFulfillmentEventService eventService) { _eventService = eventService; }

    /// <summary>Lists fulfillment events with filters and pagination.</summary>
    [HttpGet]
    [RequirePermission("fulfillment-events:read")]
    [ProducesResponseType(typeof(PaginatedResponse<FulfillmentEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchEventsAsync([FromQuery] SearchFulfillmentEventsRequest request, CancellationToken cancellationToken)
    { Result<PaginatedResponse<FulfillmentEventDto>> result = await _eventService.SearchAsync(request, cancellationToken); return ToActionResult(result); }
}
