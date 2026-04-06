using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.Inventory.API.Interfaces.Products;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Controllers;

/// <summary>
/// Handles unit of measure management operations.
/// <para>See <see cref="IUnitOfMeasureService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/units-of-measure")]
[Authorize]
public sealed class UnitsOfMeasureController : BaseApiController
{
    private readonly IUnitOfMeasureService _unitService;

    /// <summary>
    /// Initializes a new instance with the specified unit of measure service.
    /// </summary>
    public UnitsOfMeasureController(IUnitOfMeasureService unitService)
    {
        _unitService = unitService;
    }

    /// <summary>
    /// Creates a new unit of measure.
    /// </summary>
    [HttpPost]
    [RequirePermission("units-of-measure:create")]
    [ProducesResponseType(typeof(UnitOfMeasureDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUnitAsync(
        [FromBody] CreateUnitOfMeasureRequest request,
        CancellationToken cancellationToken)
    {
        Result<UnitOfMeasureDto> result = await _unitService
            .CreateAsync(request, cancellationToken);

        return ToCreatedResult(result, "GetUnitOfMeasureById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Gets a paginated list of units of measure.
    /// </summary>
    [HttpGet]
    [RequirePermission("units-of-measure:read")]
    [ProducesResponseType(typeof(PaginatedResponse<UnitOfMeasureDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUnitsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationParams.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        PaginationParams pagination = new() { Page = page, PageSize = pageSize };
        Result<PaginatedResponse<UnitOfMeasureDto>> result = await _unitService
            .ListAsync(pagination, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a unit of measure by ID.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetUnitOfMeasureById")]
    [RequirePermission("units-of-measure:read")]
    [ProducesResponseType(typeof(UnitOfMeasureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUnitByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<UnitOfMeasureDto> result = await _unitService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing unit of measure.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("units-of-measure:update")]
    [ProducesResponseType(typeof(UnitOfMeasureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUnitAsync(
        int id,
        [FromBody] UpdateUnitOfMeasureRequest request,
        CancellationToken cancellationToken)
    {
        Result<UnitOfMeasureDto> result = await _unitService
            .UpdateAsync(id, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a unit of measure if not in use.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("units-of-measure:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUnitAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _unitService.DeleteAsync(id, cancellationToken);
        return ToActionResult(result);
    }
}
