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
/// Handles zone lifecycle operations: create, search, get, update, and delete.
/// <para>See <see cref="IZoneService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/zones")]
[Authorize]
public sealed class ZonesController : BaseApiController
{
    private readonly IZoneService _zoneService;

    /// <summary>
    /// Initializes a new instance with the specified zone service.
    /// </summary>
    public ZonesController(IZoneService zoneService)
    {
        _zoneService = zoneService;
    }

    /// <summary>
    /// Creates a new zone within a warehouse.
    /// </summary>
    [HttpPost]
    [RequirePermission("zones:create")]
    [ProducesResponseType(typeof(ZoneDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateZoneAsync(
        [FromBody] CreateZoneRequest request,
        CancellationToken cancellationToken)
    {
        Result<ZoneDetailDto> result = await _zoneService
            .CreateAsync(request, cancellationToken);

        return ToCreatedResult(result, "GetZoneById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Searches zones with filters and pagination.
    /// </summary>
    [HttpGet]
    [RequirePermission("zones:read")]
    [ProducesResponseType(typeof(PaginatedResponse<ZoneDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchZonesAsync(
        [FromQuery] SearchZonesRequest request,
        CancellationToken cancellationToken)
    {
        Result<PaginatedResponse<ZoneDto>> result = await _zoneService
            .SearchAsync(request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a zone by ID with storage locations.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetZoneById")]
    [RequirePermission("zones:read")]
    [ProducesResponseType(typeof(ZoneDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetZoneByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<ZoneDetailDto> result = await _zoneService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing zone.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("zones:update")]
    [ProducesResponseType(typeof(ZoneDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateZoneAsync(
        int id,
        [FromBody] UpdateZoneRequest request,
        CancellationToken cancellationToken)
    {
        Result<ZoneDetailDto> result = await _zoneService
            .UpdateAsync(id, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a zone.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("zones:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteZoneAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _zoneService.DeleteAsync(id, cancellationToken);
        return ToActionResult(result);
    }
}
