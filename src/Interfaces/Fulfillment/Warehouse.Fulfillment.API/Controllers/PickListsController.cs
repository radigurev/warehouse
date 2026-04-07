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
/// Handles pick list operations.
/// <para>See <see cref="IPickListService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/pick-lists")]
[Authorize]
public sealed class PickListsController : BaseApiController
{
    private readonly IPickListService _pickListService;

    /// <summary>Initializes a new instance with the specified pick list service.</summary>
    public PickListsController(IPickListService pickListService) { _pickListService = pickListService; }

    /// <summary>Generates a pick list for a confirmed sales order.</summary>
    [HttpPost]
    [RequirePermission("pick-lists:create")]
    [ProducesResponseType(typeof(PickListDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GeneratePickListAsync([FromBody] GeneratePickListRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<PickListDetailDto> result = await _pickListService.GenerateAsync(request, userId, cancellationToken); return ToCreatedResult(result, "GetPickListById", dto => new { id = dto.Id }); }

    /// <summary>Lists pick lists with filters and pagination.</summary>
    [HttpGet]
    [RequirePermission("pick-lists:read")]
    [ProducesResponseType(typeof(PaginatedResponse<PickListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchPickListsAsync([FromQuery] SearchPickListsRequest request, CancellationToken cancellationToken)
    { Result<PaginatedResponse<PickListDto>> result = await _pickListService.SearchAsync(request, cancellationToken); return ToActionResult(result); }

    /// <summary>Gets a pick list by ID with all lines.</summary>
    [HttpGet("{id:int}", Name = "GetPickListById")]
    [RequirePermission("pick-lists:read")]
    [ProducesResponseType(typeof(PickListDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPickListByIdAsync(int id, CancellationToken cancellationToken)
    { Result<PickListDetailDto> result = await _pickListService.GetByIdAsync(id, cancellationToken); return ToActionResult(result); }

    /// <summary>Confirms a pick list line as picked.</summary>
    [HttpPost("{id:int}/lines/{lineId:int}/pick")]
    [RequirePermission("pick-lists:update")]
    [ProducesResponseType(typeof(PickListLineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmPickAsync(int id, int lineId, [FromBody] ConfirmPickRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<PickListLineDto> result = await _pickListService.ConfirmPickAsync(id, lineId, request, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Cancels a pending pick list.</summary>
    [HttpPost("{id:int}/cancel")]
    [RequirePermission("pick-lists:update")]
    [ProducesResponseType(typeof(PickListDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelPickListAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<PickListDetailDto> result = await _pickListService.CancelAsync(id, userId, cancellationToken); return ToActionResult(result); }
}
