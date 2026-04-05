using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.Inventory.API.Interfaces;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Controllers;

/// <summary>
/// Handles stocktake session operations: create, start, count, finalize, cancel, get, and search.
/// <para>See <see cref="IStocktakeSessionService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/stocktake-sessions")]
[Authorize]
public sealed class StocktakeSessionsController : BaseApiController
{
    private readonly IStocktakeSessionService _sessionService;

    /// <summary>
    /// Initializes a new instance with the specified stocktake session service.
    /// </summary>
    public StocktakeSessionsController(IStocktakeSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    /// <summary>
    /// Creates a new stocktake session in Draft status.
    /// </summary>
    [HttpPost]
    [RequirePermission("stocktake-sessions:create")]
    [ProducesResponseType(typeof(StocktakeSessionDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSessionAsync(
        [FromBody] CreateStocktakeSessionRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<StocktakeSessionDetailDto> result = await _sessionService
            .CreateAsync(request, userId, cancellationToken);

        return ToCreatedResult(result, "GetSessionById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Searches stocktake sessions with filters and pagination.
    /// </summary>
    [HttpGet]
    [RequirePermission("stocktake-sessions:read")]
    [ProducesResponseType(typeof(PaginatedResponse<StocktakeSessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSessionsAsync(
        [FromQuery] SearchStocktakeSessionsRequest request,
        CancellationToken cancellationToken)
    {
        Result<PaginatedResponse<StocktakeSessionDto>> result = await _sessionService
            .SearchAsync(request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a session by ID with counts.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetSessionById")]
    [RequirePermission("stocktake-sessions:read")]
    [ProducesResponseType(typeof(StocktakeSessionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<StocktakeSessionDetailDto> result = await _sessionService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Starts a draft session, transitioning to InProgress.
    /// </summary>
    [HttpPost("{id:int}/start")]
    [RequirePermission("stocktake-sessions:update")]
    [ProducesResponseType(typeof(StocktakeSessionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartSessionAsync(int id, CancellationToken cancellationToken)
    {
        Result<StocktakeSessionDetailDto> result = await _sessionService
            .StartAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Records a count entry for an in-progress session.
    /// </summary>
    [HttpPost("{sessionId:int}/counts")]
    [RequirePermission("stocktake-sessions:update")]
    [ProducesResponseType(typeof(StocktakeSessionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordCountAsync(
        int sessionId,
        [FromBody] RecordStocktakeCountRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<StocktakeSessionDetailDto> result = await _sessionService
            .RecordCountAsync(sessionId, request, userId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Finalizes an in-progress session as Completed.
    /// </summary>
    [HttpPost("{id:int}/finalize")]
    [RequirePermission("stocktake-sessions:update")]
    [ProducesResponseType(typeof(StocktakeSessionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FinalizeSessionAsync(int id, CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<StocktakeSessionDetailDto> result = await _sessionService
            .FinalizeAsync(id, userId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Cancels a draft or in-progress session.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [RequirePermission("stocktake-sessions:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelSessionAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _sessionService.CancelAsync(id, cancellationToken);
        return ToActionResult(result);
    }
}
