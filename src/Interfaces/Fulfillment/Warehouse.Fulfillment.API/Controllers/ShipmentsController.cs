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
/// Handles shipment dispatch, tracking, and search operations.
/// <para>See <see cref="IShipmentService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shipments")]
[Authorize]
public sealed class ShipmentsController : BaseApiController
{
    private readonly IShipmentService _shipmentService;

    /// <summary>Initializes a new instance with the specified shipment service.</summary>
    public ShipmentsController(IShipmentService shipmentService) { _shipmentService = shipmentService; }

    /// <summary>Creates a shipment (dispatches a packed sales order).</summary>
    [HttpPost]
    [RequirePermission("shipments:create")]
    [ProducesResponseType(typeof(ShipmentDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateShipmentAsync([FromBody] CreateShipmentRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<ShipmentDetailDto> result = await _shipmentService.CreateAsync(request, userId, cancellationToken); return ToCreatedResult(result, "GetShipmentById", dto => new { id = dto.Id }); }

    /// <summary>Lists shipments with filters and pagination.</summary>
    [HttpGet]
    [RequirePermission("shipments:read")]
    [ProducesResponseType(typeof(PaginatedResponse<ShipmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchShipmentsAsync([FromQuery] SearchShipmentsRequest request, CancellationToken cancellationToken)
    { Result<PaginatedResponse<ShipmentDto>> result = await _shipmentService.SearchAsync(request, cancellationToken); return ToActionResult(result); }

    /// <summary>Gets a shipment by ID with lines, parcels, and tracking.</summary>
    [HttpGet("{id:int}", Name = "GetShipmentById")]
    [RequirePermission("shipments:read")]
    [ProducesResponseType(typeof(ShipmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShipmentByIdAsync(int id, CancellationToken cancellationToken)
    { Result<ShipmentDetailDto> result = await _shipmentService.GetByIdAsync(id, cancellationToken); return ToActionResult(result); }

    /// <summary>Updates shipment status with tracking information.</summary>
    [HttpPost("{id:int}/status")]
    [RequirePermission("shipments:update")]
    [ProducesResponseType(typeof(ShipmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateShipmentStatusAsync(int id, [FromBody] UpdateShipmentStatusRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<ShipmentDetailDto> result = await _shipmentService.UpdateStatusAsync(id, request, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Gets the full tracking history for a shipment.</summary>
    [HttpGet("{id:int}/tracking")]
    [RequirePermission("shipments:read")]
    [ProducesResponseType(typeof(IReadOnlyList<ShipmentTrackingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrackingHistoryAsync(int id, CancellationToken cancellationToken)
    { Result<IReadOnlyList<ShipmentTrackingDto>> result = await _shipmentService.GetTrackingHistoryAsync(id, cancellationToken); return ToActionResult(result); }
}
