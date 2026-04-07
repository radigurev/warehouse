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
/// Handles sales order lifecycle operations and line management.
/// <para>See <see cref="ISalesOrderService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sales-orders")]
[Authorize]
public sealed class SalesOrdersController : BaseApiController
{
    private readonly ISalesOrderService _soService;

    /// <summary>Initializes a new instance with the specified SO service.</summary>
    public SalesOrdersController(ISalesOrderService soService) { _soService = soService; }

    /// <summary>Creates a new sales order with lines.</summary>
    [HttpPost]
    [RequirePermission("sales-orders:create")]
    [ProducesResponseType(typeof(SalesOrderDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSOAsync([FromBody] CreateSalesOrderRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<SalesOrderDetailDto> result = await _soService.CreateAsync(request, userId, cancellationToken); return ToCreatedResult(result, "GetSalesOrderById", dto => new { id = dto.Id }); }

    /// <summary>Lists sales orders with filters and pagination.</summary>
    [HttpGet]
    [RequirePermission("sales-orders:read")]
    [ProducesResponseType(typeof(PaginatedResponse<SalesOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSOsAsync([FromQuery] SearchSalesOrdersRequest request, CancellationToken cancellationToken)
    { Result<PaginatedResponse<SalesOrderDto>> result = await _soService.SearchAsync(request, cancellationToken); return ToActionResult(result); }

    /// <summary>Gets a sales order by ID with lines and progress.</summary>
    [HttpGet("{id:int}", Name = "GetSalesOrderById")]
    [RequirePermission("sales-orders:read")]
    [ProducesResponseType(typeof(SalesOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSOByIdAsync(int id, CancellationToken cancellationToken)
    { Result<SalesOrderDetailDto> result = await _soService.GetByIdAsync(id, cancellationToken); return ToActionResult(result); }

    /// <summary>Updates SO header fields (Draft only).</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("sales-orders:update")]
    [ProducesResponseType(typeof(SalesOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSOHeaderAsync(int id, [FromBody] UpdateSalesOrderRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<SalesOrderDetailDto> result = await _soService.UpdateHeaderAsync(id, request, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Confirms a sales order (Draft -> Confirmed).</summary>
    [HttpPost("{id:int}/confirm")]
    [RequirePermission("sales-orders:update")]
    [ProducesResponseType(typeof(SalesOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmSOAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<SalesOrderDetailDto> result = await _soService.ConfirmAsync(id, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Cancels a sales order.</summary>
    [HttpPost("{id:int}/cancel")]
    [RequirePermission("sales-orders:update")]
    [ProducesResponseType(typeof(SalesOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelSOAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<SalesOrderDetailDto> result = await _soService.CancelAsync(id, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Completes a sales order (Shipped -> Completed).</summary>
    [HttpPost("{id:int}/complete")]
    [RequirePermission("sales-orders:update")]
    [ProducesResponseType(typeof(SalesOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteSOAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<SalesOrderDetailDto> result = await _soService.CompleteAsync(id, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Adds a line to an SO (Draft only).</summary>
    [HttpPost("{soId:int}/lines")]
    [RequirePermission("sales-orders:update")]
    [ProducesResponseType(typeof(SalesOrderLineDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddLineAsync(int soId, [FromBody] CreateSalesOrderLineRequest request, CancellationToken cancellationToken)
    { Result<SalesOrderLineDto> result = await _soService.AddLineAsync(soId, request, cancellationToken); return ToCreatedResult(result, "GetSalesOrderById", _ => new { id = soId }); }

    /// <summary>Updates an SO line (Draft only).</summary>
    [HttpPut("{soId:int}/lines/{lineId:int}")]
    [RequirePermission("sales-orders:update")]
    [ProducesResponseType(typeof(SalesOrderLineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLineAsync(int soId, int lineId, [FromBody] UpdateSalesOrderLineRequest request, CancellationToken cancellationToken)
    { Result<SalesOrderLineDto> result = await _soService.UpdateLineAsync(soId, lineId, request, cancellationToken); return ToActionResult(result); }

    /// <summary>Removes an SO line (Draft only, cannot remove last line).</summary>
    [HttpDelete("{soId:int}/lines/{lineId:int}")]
    [RequirePermission("sales-orders:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveLineAsync(int soId, int lineId, CancellationToken cancellationToken)
    { Result result = await _soService.RemoveLineAsync(soId, lineId, cancellationToken); return ToActionResult(result); }
}
