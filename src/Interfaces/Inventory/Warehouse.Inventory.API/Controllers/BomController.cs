using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.Inventory.API.Interfaces;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Controllers;

/// <summary>
/// Handles bill of materials operations: CRUD and line management.
/// <para>See <see cref="IBomService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/bom")]
[Authorize]
public sealed class BomController : BaseApiController
{
    private readonly IBomService _bomService;

    /// <summary>
    /// Initializes a new instance with the specified BOM service.
    /// </summary>
    public BomController(IBomService bomService)
    {
        _bomService = bomService;
    }

    /// <summary>
    /// Creates a new BOM with component lines.
    /// </summary>
    [HttpPost]
    [RequirePermission("bom:create")]
    [ProducesResponseType(typeof(BomDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBomAsync(
        [FromBody] CreateBomRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<BomDto> result = await _bomService
            .CreateAsync(request, userId, cancellationToken);

        return ToCreatedResult(result, "GetBomById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Gets a BOM by ID with lines.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetBomById")]
    [RequirePermission("bom:read")]
    [ProducesResponseType(typeof(BomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBomByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<BomDto> result = await _bomService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets all BOMs for a product.
    /// </summary>
    [HttpGet("by-product/{productId:int}")]
    [RequirePermission("bom:read")]
    [ProducesResponseType(typeof(IReadOnlyList<BomDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProductAsync(int productId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<BomDto>> result = await _bomService
            .GetByProductIdAsync(productId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates a BOM header.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("bom:update")]
    [ProducesResponseType(typeof(BomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBomAsync(
        int id,
        [FromBody] UpdateBomRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<BomDto> result = await _bomService
            .UpdateAsync(id, request, userId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Adds a component line to a BOM.
    /// </summary>
    [HttpPost("{bomId:int}/lines")]
    [RequirePermission("bom:update")]
    [ProducesResponseType(typeof(BomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddBomLineAsync(
        int bomId,
        [FromBody] AddBomLineRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<BomDto> result = await _bomService
            .AddLineAsync(bomId, request, userId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Removes a component line from a BOM.
    /// </summary>
    [HttpDelete("{bomId:int}/lines/{lineId:int}")]
    [RequirePermission("bom:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveBomLineAsync(int bomId, int lineId, CancellationToken cancellationToken)
    {
        Result result = await _bomService.RemoveLineAsync(bomId, lineId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a BOM and all its lines.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("bom:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBomAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _bomService.DeleteAsync(id, cancellationToken);
        return ToActionResult(result);
    }
}
