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
/// Handles stock level query operations: get, search, and summary.
/// <para>See <see cref="IStockLevelService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/stock-levels")]
[Authorize]
public sealed class StockLevelsController : BaseApiController
{
    private readonly IStockLevelService _stockLevelService;

    /// <summary>
    /// Initializes a new instance with the specified stock level service.
    /// </summary>
    public StockLevelsController(IStockLevelService stockLevelService)
    {
        _stockLevelService = stockLevelService;
    }

    /// <summary>
    /// Searches stock levels with filters and pagination.
    /// </summary>
    [HttpGet]
    [RequirePermission("stock-levels:read")]
    [ProducesResponseType(typeof(PaginatedResponse<StockLevelDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchStockLevelsAsync(
        [FromQuery] SearchStockLevelsRequest request,
        CancellationToken cancellationToken)
    {
        Result<PaginatedResponse<StockLevelDto>> result = await _stockLevelService
            .SearchAsync(request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a stock level record by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [RequirePermission("stock-levels:read")]
    [ProducesResponseType(typeof(StockLevelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStockLevelByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<StockLevelDto> result = await _stockLevelService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets an aggregated stock summary for a product.
    /// </summary>
    [HttpGet("summary/{productId:int}")]
    [RequirePermission("stock-levels:read")]
    [ProducesResponseType(typeof(StockSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummaryByProductAsync(int productId, CancellationToken cancellationToken)
    {
        Result<StockSummaryDto> result = await _stockLevelService
            .GetSummaryByProductAsync(productId, cancellationToken);

        return ToActionResult(result);
    }
}
