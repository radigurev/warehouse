using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Controllers;

/// <summary>
/// Handles purchase order lifecycle operations.
/// <para>See <see cref="IPurchaseOrderService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/purchase-orders")]
[Authorize]
public sealed class PurchaseOrdersController : BaseApiController
{
    private readonly IPurchaseOrderService _poService;

    /// <summary>Initializes a new instance with the specified PO service.</summary>
    public PurchaseOrdersController(IPurchaseOrderService poService) { _poService = poService; }

    /// <summary>Creates a new purchase order with lines.</summary>
    [HttpPost]
    [RequirePermission("purchase-orders:create")]
    [ProducesResponseType(typeof(PurchaseOrderDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePOAsync([FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<PurchaseOrderDetailDto> result = await _poService.CreateAsync(request, userId, cancellationToken); return ToCreatedResult(result, "GetPurchaseOrderById", dto => new { id = dto.Id }); }

    /// <summary>Lists purchase orders with filters and pagination.</summary>
    [HttpGet]
    [RequirePermission("purchase-orders:read")]
    [ProducesResponseType(typeof(PaginatedResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchPOsAsync([FromQuery] SearchPurchaseOrdersRequest request, CancellationToken cancellationToken)
    { Result<PaginatedResponse<PurchaseOrderDto>> result = await _poService.SearchAsync(request, cancellationToken); return ToActionResult(result); }

    /// <summary>Gets a purchase order by ID with lines and progress.</summary>
    [HttpGet("{id:int}", Name = "GetPurchaseOrderById")]
    [RequirePermission("purchase-orders:read")]
    [ProducesResponseType(typeof(PurchaseOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPOByIdAsync(int id, CancellationToken cancellationToken)
    { Result<PurchaseOrderDetailDto> result = await _poService.GetByIdAsync(id, cancellationToken); return ToActionResult(result); }

    /// <summary>Updates PO header fields (Draft only).</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("purchase-orders:update")]
    [ProducesResponseType(typeof(PurchaseOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePOHeaderAsync(int id, [FromBody] UpdatePurchaseOrderRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<PurchaseOrderDetailDto> result = await _poService.UpdateHeaderAsync(id, request, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Confirms a purchase order (Draft -> Confirmed).</summary>
    [HttpPost("{id:int}/confirm")]
    [RequirePermission("purchase-orders:update")]
    [ProducesResponseType(typeof(PurchaseOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmPOAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<PurchaseOrderDetailDto> result = await _poService.ConfirmAsync(id, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Cancels a purchase order.</summary>
    [HttpPost("{id:int}/cancel")]
    [RequirePermission("purchase-orders:update")]
    [ProducesResponseType(typeof(PurchaseOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelPOAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<PurchaseOrderDetailDto> result = await _poService.CancelAsync(id, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Closes a purchase order.</summary>
    [HttpPost("{id:int}/close")]
    [RequirePermission("purchase-orders:update")]
    [ProducesResponseType(typeof(PurchaseOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClosePOAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<PurchaseOrderDetailDto> result = await _poService.CloseAsync(id, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Adds a line to a PO (Draft only).</summary>
    [HttpPost("{poId:int}/lines")]
    [RequirePermission("purchase-orders:update")]
    [ProducesResponseType(typeof(PurchaseOrderLineDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddLineAsync(int poId, [FromBody] CreatePurchaseOrderLineRequest request, CancellationToken cancellationToken)
    { Result<PurchaseOrderLineDto> result = await _poService.AddLineAsync(poId, request, cancellationToken); return ToCreatedResult(result, "GetPurchaseOrderById", _ => new { id = poId }); }

    /// <summary>Updates a PO line (Draft only).</summary>
    [HttpPut("{poId:int}/lines/{lineId:int}")]
    [RequirePermission("purchase-orders:update")]
    [ProducesResponseType(typeof(PurchaseOrderLineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLineAsync(int poId, int lineId, [FromBody] UpdatePurchaseOrderLineRequest request, CancellationToken cancellationToken)
    { Result<PurchaseOrderLineDto> result = await _poService.UpdateLineAsync(poId, lineId, request, cancellationToken); return ToActionResult(result); }

    /// <summary>Removes a PO line (Draft only, cannot remove last line).</summary>
    [HttpDelete("{poId:int}/lines/{lineId:int}")]
    [RequirePermission("purchase-orders:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveLineAsync(int poId, int lineId, CancellationToken cancellationToken)
    { Result result = await _poService.RemoveLineAsync(poId, lineId, cancellationToken); return ToActionResult(result); }
}
