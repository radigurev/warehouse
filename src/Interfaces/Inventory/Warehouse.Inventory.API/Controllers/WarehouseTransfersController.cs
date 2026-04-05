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
/// Handles warehouse transfer operations: create, complete, cancel, get, and search.
/// <para>See <see cref="IWarehouseTransferService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/warehouse-transfers")]
[Authorize]
public sealed class WarehouseTransfersController : BaseApiController
{
    private readonly IWarehouseTransferService _transferService;

    /// <summary>
    /// Initializes a new instance with the specified transfer service.
    /// </summary>
    public WarehouseTransfersController(IWarehouseTransferService transferService)
    {
        _transferService = transferService;
    }

    /// <summary>
    /// Creates a new warehouse transfer in Draft status.
    /// </summary>
    [HttpPost]
    [RequirePermission("warehouse-transfers:create")]
    [ProducesResponseType(typeof(WarehouseTransferDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTransferAsync(
        [FromBody] CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<WarehouseTransferDetailDto> result = await _transferService
            .CreateAsync(request, userId, cancellationToken);

        return ToCreatedResult(result, "GetTransferById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Searches transfers with filters and pagination.
    /// </summary>
    [HttpGet]
    [RequirePermission("warehouse-transfers:read")]
    [ProducesResponseType(typeof(PaginatedResponse<WarehouseTransferDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTransfersAsync(
        [FromQuery] SearchTransfersRequest request,
        CancellationToken cancellationToken)
    {
        Result<PaginatedResponse<WarehouseTransferDto>> result = await _transferService
            .SearchAsync(request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a transfer by ID with lines.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetTransferById")]
    [RequirePermission("warehouse-transfers:read")]
    [ProducesResponseType(typeof(WarehouseTransferDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransferByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<WarehouseTransferDetailDto> result = await _transferService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Completes a draft transfer, moving stock between warehouses.
    /// </summary>
    [HttpPost("{id:int}/complete")]
    [RequirePermission("warehouse-transfers:update")]
    [ProducesResponseType(typeof(WarehouseTransferDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteTransferAsync(
        int id,
        [FromBody] CompleteTransferRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<WarehouseTransferDetailDto> result = await _transferService
            .CompleteAsync(id, request, userId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Cancels a draft transfer.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [RequirePermission("warehouse-transfers:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelTransferAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _transferService.CancelAsync(id, cancellationToken);
        return ToActionResult(result);
    }
}
