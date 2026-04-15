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
/// Handles goods receipt and receiving inspection operations.
/// <para>See <see cref="IGoodsReceiptService"/>, <see cref="IReceivingInspectionService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/goods-receipts")]
[Authorize]
public sealed class GoodsReceiptsController : BaseApiController
{
    private readonly IGoodsReceiptService _receiptService;
    private readonly IReceivingInspectionService _inspectionService;

    /// <summary>Initializes a new instance with the specified services.</summary>
    public GoodsReceiptsController(IGoodsReceiptService receiptService, IReceivingInspectionService inspectionService)
    { _receiptService = receiptService; _inspectionService = inspectionService; }

    /// <summary>Creates a new goods receipt with lines.</summary>
    [HttpPost]
    [RequirePermission("goods-receipts:create")]
    [ProducesResponseType(typeof(GoodsReceiptDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReceiptAsync([FromBody] CreateGoodsReceiptRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<GoodsReceiptDetailDto> result = await _receiptService.CreateAsync(request, userId, cancellationToken); return ToCreatedResult(result, "GetGoodsReceiptById", dto => new { id = dto.Id }); }

    /// <summary>Lists goods receipts with filters and pagination.</summary>
    [HttpGet]
    [RequirePermission("goods-receipts:read")]
    [ProducesResponseType(typeof(PaginatedResponse<GoodsReceiptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchReceiptsAsync([FromQuery] SearchGoodsReceiptsRequest request, CancellationToken cancellationToken)
    { Result<PaginatedResponse<GoodsReceiptDto>> result = await _receiptService.SearchAsync(request, cancellationToken); return ToActionResult(result); }

    /// <summary>Gets a goods receipt by ID with all lines.</summary>
    [HttpGet("{id:int}", Name = "GetGoodsReceiptById")]
    [RequirePermission("goods-receipts:read")]
    [ProducesResponseType(typeof(GoodsReceiptDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReceiptByIdAsync(int id, CancellationToken cancellationToken)
    { Result<GoodsReceiptDetailDto> result = await _receiptService.GetByIdAsync(id, cancellationToken); return ToActionResult(result); }

    /// <summary>Completes a goods receipt, publishes event, and updates PO status.</summary>
    [HttpPost("{id:int}/complete")]
    [RequirePermission("goods-receipts:update")]
    [ProducesResponseType(typeof(GoodsReceiptDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteReceiptAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<GoodsReceiptDetailDto> result = await _receiptService.CompleteAsync(id, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Inspects a receipt line (sets Accepted, Rejected, or Quarantined).</summary>
    [HttpPost("{receiptId:int}/lines/{lineId:int}/inspect")]
    [RequirePermission("goods-receipts:update")]
    [ProducesResponseType(typeof(GoodsReceiptLineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InspectLineAsync(int receiptId, int lineId, [FromBody] InspectLineRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<GoodsReceiptLineDto> result = await _inspectionService.InspectAsync(receiptId, lineId, request, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Resolves a quarantined receipt line (Accepted or Rejected).</summary>
    [HttpPost("{receiptId:int}/lines/{lineId:int}/resolve")]
    [RequirePermission("goods-receipts:update")]
    [ProducesResponseType(typeof(GoodsReceiptLineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResolveQuarantineAsync(int receiptId, int lineId, [FromBody] ResolveQuarantineRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<GoodsReceiptLineDto> result = await _inspectionService.ResolveQuarantineAsync(receiptId, lineId, request, userId, cancellationToken); return ToActionResult(result); }

}
