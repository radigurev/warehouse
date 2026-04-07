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
/// Handles supplier return operations.
/// <para>See <see cref="ISupplierReturnService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/supplier-returns")]
[Authorize]
public sealed class SupplierReturnsController : BaseApiController
{
    private readonly ISupplierReturnService _returnService;

    /// <summary>Initializes a new instance with the specified return service.</summary>
    public SupplierReturnsController(ISupplierReturnService returnService) { _returnService = returnService; }

    /// <summary>Creates a new supplier return with lines.</summary>
    [HttpPost]
    [RequirePermission("supplier-returns:create")]
    [ProducesResponseType(typeof(SupplierReturnDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReturnAsync([FromBody] CreateSupplierReturnRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<SupplierReturnDetailDto> result = await _returnService.CreateAsync(request, userId, cancellationToken); return ToCreatedResult(result, "GetSupplierReturnById", dto => new { id = dto.Id }); }

    /// <summary>Lists supplier returns with filters and pagination.</summary>
    [HttpGet]
    [RequirePermission("supplier-returns:read")]
    [ProducesResponseType(typeof(PaginatedResponse<SupplierReturnDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchReturnsAsync([FromQuery] SearchSupplierReturnsRequest request, CancellationToken cancellationToken)
    { Result<PaginatedResponse<SupplierReturnDto>> result = await _returnService.SearchAsync(request, cancellationToken); return ToActionResult(result); }

    /// <summary>Gets a supplier return by ID with all lines.</summary>
    [HttpGet("{id:int}", Name = "GetSupplierReturnById")]
    [RequirePermission("supplier-returns:read")]
    [ProducesResponseType(typeof(SupplierReturnDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReturnByIdAsync(int id, CancellationToken cancellationToken)
    { Result<SupplierReturnDetailDto> result = await _returnService.GetByIdAsync(id, cancellationToken); return ToActionResult(result); }

    /// <summary>Confirms a supplier return (publishes event).</summary>
    [HttpPost("{id:int}/confirm")]
    [RequirePermission("supplier-returns:update")]
    [ProducesResponseType(typeof(SupplierReturnDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmReturnAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<SupplierReturnDetailDto> result = await _returnService.ConfirmAsync(id, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Cancels a draft supplier return.</summary>
    [HttpPost("{id:int}/cancel")]
    [RequirePermission("supplier-returns:update")]
    [ProducesResponseType(typeof(SupplierReturnDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelReturnAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<SupplierReturnDetailDto> result = await _returnService.CancelAsync(id, userId, cancellationToken); return ToActionResult(result); }
}
