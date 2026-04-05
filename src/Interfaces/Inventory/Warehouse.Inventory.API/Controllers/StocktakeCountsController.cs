using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.Inventory.API.Interfaces.Stocktake;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Controllers;

/// <summary>
/// Handles stocktake count operations: add, update, delete, and list entries for a session.
/// <para>See <see cref="IStocktakeCountService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/stocktake-sessions/{sessionId:int}/counts")]
[Authorize]
public sealed class StocktakeCountsController : BaseApiController
{
    private readonly IStocktakeCountService _countService;

    /// <summary>
    /// Initializes a new instance with the specified stocktake count service.
    /// </summary>
    public StocktakeCountsController(IStocktakeCountService countService)
    {
        _countService = countService;
    }

    /// <summary>
    /// Adds a count entry to an in-progress session.
    /// </summary>
    [HttpPost]
    [RequirePermission("stocktake:update")]
    [ProducesResponseType(typeof(StocktakeCountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddCountAsync(
        int sessionId,
        [FromBody] RecordStocktakeCountRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<StocktakeCountDto> result = await _countService
            .AddAsync(sessionId, request, userId, cancellationToken);

        return ToCreatedResult(result, "GetCountsBySession", _ => new { sessionId });
    }

    /// <summary>
    /// Lists all count entries for a stocktake session.
    /// </summary>
    [HttpGet(Name = "GetCountsBySession")]
    [RequirePermission("stocktake:read")]
    [ProducesResponseType(typeof(IReadOnlyList<StocktakeCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListCountsAsync(int sessionId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<StocktakeCountDto>> result = await _countService
            .ListBySessionAsync(sessionId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates the actual quantity on an existing count entry.
    /// </summary>
    [HttpPut("{countId:int}")]
    [RequirePermission("stocktake:update")]
    [ProducesResponseType(typeof(StocktakeCountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCountAsync(
        int sessionId,
        int countId,
        [FromBody] UpdateStocktakeCountRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<StocktakeCountDto> result = await _countService
            .UpdateAsync(sessionId, countId, request, userId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a count entry from an in-progress session.
    /// </summary>
    [HttpDelete("{countId:int}")]
    [RequirePermission("stocktake:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCountAsync(
        int sessionId,
        int countId,
        CancellationToken cancellationToken)
    {
        Result result = await _countService.DeleteAsync(sessionId, countId, cancellationToken);
        return ToActionResult(result);
    }
}
