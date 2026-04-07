using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Controllers;

/// <summary>
/// Handles carrier management and service level operations.
/// <para>See <see cref="ICarrierService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/carriers")]
[Authorize]
public sealed class CarriersController : BaseApiController
{
    private readonly ICarrierService _carrierService;

    /// <summary>Initializes a new instance with the specified carrier service.</summary>
    public CarriersController(ICarrierService carrierService) { _carrierService = carrierService; }

    /// <summary>Creates a new carrier.</summary>
    [HttpPost]
    [RequirePermission("carriers:create")]
    [ProducesResponseType(typeof(CarrierDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCarrierAsync([FromBody] CreateCarrierRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<CarrierDetailDto> result = await _carrierService.CreateAsync(request, userId, cancellationToken); return ToCreatedResult(result, "GetCarrierById", dto => new { id = dto.Id }); }

    /// <summary>Lists carriers with filters and pagination.</summary>
    [HttpGet]
    [RequirePermission("carriers:read")]
    [ProducesResponseType(typeof(PaginatedResponse<CarrierDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchCarriersAsync([FromQuery] SearchCarriersRequest request, CancellationToken cancellationToken)
    { Result<PaginatedResponse<CarrierDto>> result = await _carrierService.SearchAsync(request, cancellationToken); return ToActionResult(result); }

    /// <summary>Gets a carrier by ID with service levels.</summary>
    [HttpGet("{id:int}", Name = "GetCarrierById")]
    [RequirePermission("carriers:read")]
    [ProducesResponseType(typeof(CarrierDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCarrierByIdAsync(int id, CancellationToken cancellationToken)
    { Result<CarrierDetailDto> result = await _carrierService.GetByIdAsync(id, cancellationToken); return ToActionResult(result); }

    /// <summary>Updates a carrier.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("carriers:update")]
    [ProducesResponseType(typeof(CarrierDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCarrierAsync(int id, [FromBody] UpdateCarrierRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<CarrierDetailDto> result = await _carrierService.UpdateAsync(id, request, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Deactivates a carrier.</summary>
    [HttpPost("{id:int}/deactivate")]
    [RequirePermission("carriers:update")]
    [ProducesResponseType(typeof(CarrierDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateCarrierAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<CarrierDetailDto> result = await _carrierService.DeactivateAsync(id, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Creates a service level for a carrier.</summary>
    [HttpPost("{carrierId:int}/service-levels")]
    [RequirePermission("carriers:update")]
    [ProducesResponseType(typeof(CarrierServiceLevelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateServiceLevelAsync(int carrierId, [FromBody] CreateCarrierServiceLevelRequest request, CancellationToken cancellationToken)
    { Result<CarrierServiceLevelDto> result = await _carrierService.CreateServiceLevelAsync(carrierId, request, cancellationToken); return ToCreatedResult(result, "GetCarrierById", _ => new { id = carrierId }); }

    /// <summary>Lists service levels for a carrier.</summary>
    [HttpGet("{carrierId:int}/service-levels")]
    [RequirePermission("carriers:read")]
    [ProducesResponseType(typeof(IReadOnlyList<CarrierServiceLevelDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListServiceLevelsAsync(int carrierId, CancellationToken cancellationToken)
    { Result<IReadOnlyList<CarrierServiceLevelDto>> result = await _carrierService.ListServiceLevelsAsync(carrierId, cancellationToken); return ToActionResult(result); }

    /// <summary>Updates a carrier service level.</summary>
    [HttpPut("{carrierId:int}/service-levels/{levelId:int}")]
    [RequirePermission("carriers:update")]
    [ProducesResponseType(typeof(CarrierServiceLevelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateServiceLevelAsync(int carrierId, int levelId, [FromBody] UpdateCarrierServiceLevelRequest request, CancellationToken cancellationToken)
    { Result<CarrierServiceLevelDto> result = await _carrierService.UpdateServiceLevelAsync(carrierId, levelId, request, cancellationToken); return ToActionResult(result); }

    /// <summary>Deletes a carrier service level.</summary>
    [HttpDelete("{carrierId:int}/service-levels/{levelId:int}")]
    [RequirePermission("carriers:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteServiceLevelAsync(int carrierId, int levelId, CancellationToken cancellationToken)
    { Result result = await _carrierService.DeleteServiceLevelAsync(carrierId, levelId, cancellationToken); return ToActionResult(result); }
}
