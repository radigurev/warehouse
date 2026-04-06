using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.Inventory.API.Interfaces.Stocktake;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Controllers;

/// <summary>
/// Handles stocktake session lifecycle operations: create, start, complete, cancel, get, and search.
/// <para>See <see cref="IStocktakeSessionService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/stocktake")]
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
    [RequirePermission("stocktake:create")]
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
    [RequirePermission("stocktake:read")]
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
    [RequirePermission("stocktake:read")]
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
    [RequirePermission("stocktake:update")]
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
    /// Completes an in-progress session, setting status to Completed.
    /// </summary>
    [HttpPost("{id:int}/complete")]
    [RequirePermission("stocktake:finalize")]
    [ProducesResponseType(typeof(StocktakeSessionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteSessionAsync(int id, CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<StocktakeSessionDetailDto> result = await _sessionService
            .CompleteAsync(id, userId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Cancels a draft or in-progress session.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [RequirePermission("stocktake:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelSessionAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _sessionService.CancelAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Returns a variance report for a completed stocktake session.
    /// </summary>
    [HttpGet("{id:int}/variance-report")]
    [RequirePermission("stocktake:read")]
    [ProducesResponseType(typeof(IReadOnlyList<StocktakeCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetVarianceReportAsync(int id, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<StocktakeCountDto>> result = await _sessionService
            .GetVarianceReportAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Creates an inventory adjustment from the variances of a completed session.
    /// </summary>
    [HttpPost("{id:int}/create-adjustment")]
    [RequirePermission("stocktake:update")]
    [ProducesResponseType(typeof(InventoryAdjustmentDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAdjustmentFromSessionAsync(int id, CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<InventoryAdjustmentDetailDto> result = await _sessionService
            .CreateAdjustmentFromSessionAsync(id, userId, cancellationToken);

        if (result.IsSuccess)
            return StatusCode(StatusCodes.Status201Created, result.Value);

        return ToActionResult(result);
    }
}
