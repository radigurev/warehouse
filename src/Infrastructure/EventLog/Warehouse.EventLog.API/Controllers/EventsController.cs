using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.EventLog.API.Services.Interfaces;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.ServiceModel.DTOs.EventLog;
using Warehouse.ServiceModel.Requests.EventLog;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.EventLog.API.Controllers;

/// <summary>
/// Read-only API for querying centralized operations events across all ISA-95 domains.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/events")]
[Authorize]
public sealed class EventsController : BaseApiController
{
    private readonly IEventQueryService _eventQueryService;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public EventsController(IEventQueryService eventQueryService)
    {
        _eventQueryService = eventQueryService;
    }

    /// <summary>
    /// Searches operations events across all domains with optional filters and pagination.
    /// </summary>
    [HttpGet]
    [RequirePermission("events:read")]
    [ProducesResponseType(typeof(PaginatedResponse<OperationsEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SearchEventsAsync(
        [FromQuery] SearchEventsRequest request,
        CancellationToken cancellationToken)
    {
        Result<PaginatedResponse<OperationsEventDto>> result = await _eventQueryService
            .SearchAsync(request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a single operations event by ID with full payload.
    /// </summary>
    [HttpGet("{id:long}", Name = "GetEventById")]
    [RequirePermission("events:read")]
    [ProducesResponseType(typeof(OperationsEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEventByIdAsync(long id, CancellationToken cancellationToken)
    {
        Result<OperationsEventDto> result = await _eventQueryService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Lists distinct event types, optionally filtered by domain.
    /// </summary>
    [HttpGet("types")]
    [RequirePermission("events:read")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEventTypesAsync(
        [FromQuery] string? domain,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<string>> result = await _eventQueryService
            .GetEventTypesAsync(domain, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Lists distinct entity types, optionally filtered by domain.
    /// </summary>
    [HttpGet("entity-types")]
    [RequirePermission("events:read")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEntityTypesAsync(
        [FromQuery] string? domain,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<string>> result = await _eventQueryService
            .GetEntityTypesAsync(domain, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets all events sharing a correlation ID, sorted chronologically (ascending).
    /// </summary>
    [HttpGet("correlation/{correlationId}")]
    [RequirePermission("events:read")]
    [ProducesResponseType(typeof(IReadOnlyList<OperationsEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCorrelationTimelineAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<OperationsEventDto>> result = await _eventQueryService
            .GetCorrelationTimelineAsync(correlationId, cancellationToken);

        return ToActionResult(result);
    }
}
