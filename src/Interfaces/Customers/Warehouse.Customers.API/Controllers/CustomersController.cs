using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Customers.API.Interfaces;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Customers.API.Controllers;

/// <summary>
/// Handles customer lifecycle operations: create, search, get, update, deactivate, and reactivate.
/// <para>See <see cref="ICustomerService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customers")]
[Authorize]
public sealed class CustomersController : BaseApiController
{
    private readonly ICustomerService _customerService;

    /// <summary>
    /// Initializes a new instance with the specified customer service.
    /// </summary>
    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Creates a new customer. Auto-generates code if not provided.
    /// </summary>
    [HttpPost]
    [RequirePermission("customers:write")]
    [ProducesResponseType(typeof(CustomerDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCustomerAsync(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<CustomerDetailDto> result = await _customerService
            .CreateAsync(request, userId, cancellationToken);

        return ToCreatedResult(result, "GetCustomerById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Searches customers with filters and pagination.
    /// </summary>
    [HttpGet]
    [RequirePermission("customers:read")]
    [ProducesResponseType(typeof(PaginatedResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SearchCustomersAsync(
        [FromQuery] SearchCustomersRequest request,
        CancellationToken cancellationToken)
    {
        Result<PaginatedResponse<CustomerDto>> result = await _customerService
            .SearchAsync(request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a customer by ID with all nested data.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetCustomerById")]
    [RequirePermission("customers:read")]
    [ProducesResponseType(typeof(CustomerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<CustomerDetailDto> result = await _customerService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing customer's fields.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(typeof(CustomerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCustomerAsync(
        int id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<CustomerDetailDto> result = await _customerService
            .UpdateAsync(id, request, userId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Soft-deletes (deactivates) a customer.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("customers:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateCustomerAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _customerService.DeactivateAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Reactivates a soft-deleted customer.
    /// </summary>
    [HttpPost("{id:int}/reactivate")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(typeof(CustomerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateCustomerAsync(int id, CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<CustomerDetailDto> result = await _customerService
            .ReactivateAsync(id, userId, cancellationToken);

        return ToActionResult(result);
    }
}
