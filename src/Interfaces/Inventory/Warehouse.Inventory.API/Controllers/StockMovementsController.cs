using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.Inventory.API.Interfaces.Stock;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Controllers;

/// <summary>
/// Handles stock movement operations: record and search.
/// <para>See <see cref="IStockMovementService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/stock-movements")]
[Authorize]
public sealed class StockMovementsController : BaseApiController
{
    private readonly IStockMovementService _movementService;

    /// <summary>
    /// Initializes a new instance with the specified stock movement service.
    /// </summary>
    public StockMovementsController(IStockMovementService movementService)
    {
        _movementService = movementService;
    }

    /// <summary>
    /// Records a new stock movement.
    /// </summary>
    [HttpPost]
    [RequirePermission("stock-movements:create")]
    [ProducesResponseType(typeof(StockMovementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordMovementAsync(
        [FromBody] RecordStockMovementRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<StockMovementDto> result = await _movementService
            .RecordAsync(request, userId, cancellationToken);

        if (result.IsSuccess)
            return StatusCode(StatusCodes.Status201Created, result.Value);

        return ToActionResult(result);
    }

    /// <summary>
    /// Searches stock movements with filters and pagination.
    /// </summary>
    [HttpGet]
    [RequirePermission("stock-movements:read")]
    [ProducesResponseType(typeof(PaginatedResponse<StockMovementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchMovementsAsync(
        [FromQuery] SearchStockMovementsRequest request,
        CancellationToken cancellationToken)
    {
        Result<PaginatedResponse<StockMovementDto>> result = await _movementService
            .SearchAsync(request, cancellationToken);

        return ToActionResult(result);
    }
}
