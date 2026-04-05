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
/// Handles inventory adjustment operations: create, approve, reject, apply, get, and search.
/// <para>See <see cref="IInventoryAdjustmentService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/inventory-adjustments")]
[Authorize]
public sealed class InventoryAdjustmentsController : BaseApiController
{
    private readonly IInventoryAdjustmentService _adjustmentService;

    /// <summary>
    /// Initializes a new instance with the specified adjustment service.
    /// </summary>
    public InventoryAdjustmentsController(IInventoryAdjustmentService adjustmentService)
    {
        _adjustmentService = adjustmentService;
    }

    /// <summary>
    /// Creates a new inventory adjustment in Pending status.
    /// </summary>
    [HttpPost]
    [RequirePermission("inventory-adjustments:create")]
    [ProducesResponseType(typeof(InventoryAdjustmentDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAdjustmentAsync(
        [FromBody] CreateAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<InventoryAdjustmentDetailDto> result = await _adjustmentService
            .CreateAsync(request, userId, cancellationToken);

        return ToCreatedResult(result, "GetAdjustmentById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Searches adjustments with filters and pagination.
    /// </summary>
    [HttpGet]
    [RequirePermission("inventory-adjustments:read")]
    [ProducesResponseType(typeof(PaginatedResponse<InventoryAdjustmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAdjustmentsAsync(
        [FromQuery] SearchAdjustmentsRequest request,
        CancellationToken cancellationToken)
    {
        Result<PaginatedResponse<InventoryAdjustmentDto>> result = await _adjustmentService
            .SearchAsync(request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets an adjustment by ID with lines and approval details.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetAdjustmentById")]
    [RequirePermission("inventory-adjustments:read")]
    [ProducesResponseType(typeof(InventoryAdjustmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAdjustmentByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<InventoryAdjustmentDetailDto> result = await _adjustmentService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Approves a pending adjustment.
    /// </summary>
    [HttpPost("{id:int}/approve")]
    [RequirePermission("inventory-adjustments:approve")]
    [ProducesResponseType(typeof(InventoryAdjustmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveAdjustmentAsync(
        int id,
        [FromBody] ApproveAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<InventoryAdjustmentDetailDto> result = await _adjustmentService
            .ApproveAsync(id, request, userId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Rejects a pending adjustment.
    /// </summary>
    [HttpPost("{id:int}/reject")]
    [RequirePermission("inventory-adjustments:approve")]
    [ProducesResponseType(typeof(InventoryAdjustmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RejectAdjustmentAsync(
        int id,
        [FromBody] RejectAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<InventoryAdjustmentDetailDto> result = await _adjustmentService
            .RejectAsync(id, request, userId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Applies an approved adjustment to stock levels.
    /// </summary>
    [HttpPost("{id:int}/apply")]
    [RequirePermission("inventory-adjustments:apply")]
    [ProducesResponseType(typeof(InventoryAdjustmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApplyAdjustmentAsync(int id, CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<InventoryAdjustmentDetailDto> result = await _adjustmentService
            .ApplyAsync(id, userId, cancellationToken);

        return ToActionResult(result);
    }
}
