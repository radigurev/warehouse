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
/// Handles customer return (RMA) operations.
/// <para>See <see cref="ICustomerReturnService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customer-returns")]
[Authorize]
public sealed class CustomerReturnsController : BaseApiController
{
    private readonly ICustomerReturnService _returnService;

    /// <summary>Initializes a new instance with the specified return service.</summary>
    public CustomerReturnsController(ICustomerReturnService returnService) { _returnService = returnService; }

    /// <summary>Creates a customer return with lines.</summary>
    [HttpPost]
    [RequirePermission("customer-returns:create")]
    [ProducesResponseType(typeof(CustomerReturnDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateReturnAsync([FromBody] CreateCustomerReturnRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<CustomerReturnDetailDto> result = await _returnService.CreateAsync(request, userId, cancellationToken); return ToCreatedResult(result, "GetCustomerReturnById", dto => new { id = dto.Id }); }

    /// <summary>Lists customer returns with filters and pagination.</summary>
    [HttpGet]
    [RequirePermission("customer-returns:read")]
    [ProducesResponseType(typeof(PaginatedResponse<CustomerReturnDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchReturnsAsync([FromQuery] SearchCustomerReturnsRequest request, CancellationToken cancellationToken)
    { Result<PaginatedResponse<CustomerReturnDto>> result = await _returnService.SearchAsync(request, cancellationToken); return ToActionResult(result); }

    /// <summary>Gets a customer return by ID with lines.</summary>
    [HttpGet("{id:int}", Name = "GetCustomerReturnById")]
    [RequirePermission("customer-returns:read")]
    [ProducesResponseType(typeof(CustomerReturnDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReturnByIdAsync(int id, CancellationToken cancellationToken)
    { Result<CustomerReturnDetailDto> result = await _returnService.GetByIdAsync(id, cancellationToken); return ToActionResult(result); }

    /// <summary>Confirms a customer return (Draft -> Confirmed).</summary>
    [HttpPost("{id:int}/confirm")]
    [RequirePermission("customer-returns:update")]
    [ProducesResponseType(typeof(CustomerReturnDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmReturnAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<CustomerReturnDetailDto> result = await _returnService.ConfirmAsync(id, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Receives a customer return (Confirmed -> Received).</summary>
    [HttpPost("{id:int}/receive")]
    [RequirePermission("customer-returns:update")]
    [ProducesResponseType(typeof(CustomerReturnDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReceiveReturnAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<CustomerReturnDetailDto> result = await _returnService.ReceiveAsync(id, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Closes a customer return (Received -> Closed).</summary>
    [HttpPost("{id:int}/close")]
    [RequirePermission("customer-returns:update")]
    [ProducesResponseType(typeof(CustomerReturnDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CloseReturnAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<CustomerReturnDetailDto> result = await _returnService.CloseAsync(id, userId, cancellationToken); return ToActionResult(result); }

    /// <summary>Cancels a customer return (Draft/Confirmed -> Cancelled).</summary>
    [HttpPost("{id:int}/cancel")]
    [RequirePermission("customer-returns:update")]
    [ProducesResponseType(typeof(CustomerReturnDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelReturnAsync(int id, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<CustomerReturnDetailDto> result = await _returnService.CancelAsync(id, userId, cancellationToken); return ToActionResult(result); }
}
