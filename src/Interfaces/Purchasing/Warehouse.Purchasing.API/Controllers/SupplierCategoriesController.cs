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
/// Handles supplier category management operations.
/// <para>See <see cref="ISupplierCategoryService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/supplier-categories")]
[Authorize]
public sealed class SupplierCategoriesController : BaseApiController
{
    private readonly ISupplierCategoryService _categoryService;

    /// <summary>
    /// Initializes a new instance with the specified category service.
    /// </summary>
    public SupplierCategoriesController(ISupplierCategoryService categoryService) { _categoryService = categoryService; }

    /// <summary>Creates a new supplier category.</summary>
    [HttpPost]
    [RequirePermission("suppliers:create")]
    [ProducesResponseType(typeof(SupplierCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategoryAsync([FromBody] CreateSupplierCategoryRequest request, CancellationToken cancellationToken)
    {
        Result<SupplierCategoryDto> result = await _categoryService.CreateAsync(request, cancellationToken);
        return ToCreatedResult(result, "GetSupplierCategoryById", dto => new { id = dto.Id });
    }

    /// <summary>Gets a paginated list of supplier categories.</summary>
    [HttpGet]
    [RequirePermission("suppliers:read")]
    [ProducesResponseType(typeof(PaginatedResponse<SupplierCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCategoriesAsync([FromQuery] int page = 1, [FromQuery] int pageSize = PaginationParams.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        PaginationParams pagination = new() { Page = page, PageSize = pageSize };
        Result<PaginatedResponse<SupplierCategoryDto>> result = await _categoryService.GetAllAsync(pagination, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Gets a supplier category by ID.</summary>
    [HttpGet("{id:int}", Name = "GetSupplierCategoryById")]
    [RequirePermission("suppliers:read")]
    [ProducesResponseType(typeof(SupplierCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<SupplierCategoryDto> result = await _categoryService.GetByIdAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Updates an existing supplier category.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("suppliers:update")]
    [ProducesResponseType(typeof(SupplierCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategoryAsync(int id, [FromBody] UpdateSupplierCategoryRequest request, CancellationToken cancellationToken)
    {
        Result<SupplierCategoryDto> result = await _categoryService.UpdateAsync(id, request, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Deletes a supplier category if not in use.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("suppliers:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategoryAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _categoryService.DeleteAsync(id, cancellationToken);
        return ToActionResult(result);
    }
}
