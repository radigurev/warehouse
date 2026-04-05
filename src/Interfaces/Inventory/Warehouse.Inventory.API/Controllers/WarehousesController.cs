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
/// Handles warehouse management operations: create, list, get, update, and deactivate.
/// <para>See <see cref="IWarehouseService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/warehouses")]
[Authorize]
public sealed class WarehousesController : BaseApiController
{
    private readonly IWarehouseService _warehouseService;

    /// <summary>
    /// Initializes a new instance with the specified warehouse service.
    /// </summary>
    public WarehousesController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    /// <summary>
    /// Creates a new warehouse.
    /// </summary>
    [HttpPost]
    [RequirePermission("warehouses:create")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateWarehouseAsync(
        [FromBody] CreateWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<WarehouseDto> result = await _warehouseService
            .CreateAsync(request, userId, cancellationToken);

        return ToCreatedResult(result, "GetWarehouseById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Lists warehouses with pagination.
    /// </summary>
    [HttpGet]
    [RequirePermission("warehouses:read")]
    [ProducesResponseType(typeof(PaginatedResponse<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListWarehousesAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        Result<PaginatedResponse<WarehouseDto>> result = await _warehouseService
            .SearchAsync(page, pageSize, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a warehouse by ID.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetWarehouseById")]
    [RequirePermission("warehouses:read")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWarehouseByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<WarehouseDto> result = await _warehouseService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing warehouse.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("warehouses:update")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWarehouseAsync(
        int id,
        [FromBody] UpdateWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<WarehouseDto> result = await _warehouseService
            .UpdateAsync(id, request, userId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Soft-deletes (deactivates) a warehouse.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("warehouses:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateWarehouseAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _warehouseService.DeactivateAsync(id, cancellationToken);
        return ToActionResult(result);
    }
}
