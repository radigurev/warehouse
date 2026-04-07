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
/// Handles supplier lifecycle operations: create, search, get, update, deactivate, and reactivate.
/// <para>See <see cref="ISupplierService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/suppliers")]
[Authorize]
public sealed class SuppliersController : BaseApiController
{
    private readonly ISupplierService _supplierService;

    /// <summary>
    /// Initializes a new instance with the specified supplier service.
    /// </summary>
    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    /// <summary>
    /// Creates a new supplier. Auto-generates code if not provided.
    /// </summary>
    [HttpPost]
    [RequirePermission("suppliers:create")]
    [ProducesResponseType(typeof(SupplierDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSupplierAsync([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();
        Result<SupplierDetailDto> result = await _supplierService.CreateAsync(request, userId, cancellationToken);
        return ToCreatedResult(result, "GetSupplierById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Searches suppliers with filters and pagination.
    /// </summary>
    [HttpGet]
    [RequirePermission("suppliers:read")]
    [ProducesResponseType(typeof(PaginatedResponse<SupplierDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSuppliersAsync([FromQuery] SearchSuppliersRequest request, CancellationToken cancellationToken)
    {
        Result<PaginatedResponse<SupplierDto>> result = await _supplierService.SearchAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a supplier by ID with all nested data.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetSupplierById")]
    [RequirePermission("suppliers:read")]
    [ProducesResponseType(typeof(SupplierDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSupplierByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<SupplierDetailDto> result = await _supplierService.GetByIdAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing supplier's fields.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("suppliers:update")]
    [ProducesResponseType(typeof(SupplierDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSupplierAsync(int id, [FromBody] UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();
        Result<SupplierDetailDto> result = await _supplierService.UpdateAsync(id, request, userId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Soft-deletes (deactivates) a supplier.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("suppliers:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateSupplierAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _supplierService.DeactivateAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Reactivates a soft-deleted supplier.
    /// </summary>
    [HttpPost("{id:int}/reactivate")]
    [RequirePermission("suppliers:update")]
    [ProducesResponseType(typeof(SupplierDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateSupplierAsync(int id, CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();
        Result<SupplierDetailDto> result = await _supplierService.ReactivateAsync(id, userId, cancellationToken);
        return ToActionResult(result);
    }
}
