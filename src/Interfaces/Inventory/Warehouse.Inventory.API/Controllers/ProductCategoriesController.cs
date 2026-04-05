using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.Inventory.API.Interfaces;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Controllers;

/// <summary>
/// Handles product category management operations.
/// <para>See <see cref="IProductCategoryService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/product-categories")]
[Authorize]
public sealed class ProductCategoriesController : BaseApiController
{
    private readonly IProductCategoryService _categoryService;

    /// <summary>
    /// Initializes a new instance with the specified category service.
    /// </summary>
    public ProductCategoriesController(IProductCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Creates a new product category.
    /// </summary>
    [HttpPost]
    [RequirePermission("product-categories:create")]
    [ProducesResponseType(typeof(ProductCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategoryAsync(
        [FromBody] CreateProductCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Result<ProductCategoryDto> result = await _categoryService
            .CreateAsync(request, cancellationToken);

        return ToCreatedResult(result, "GetProductCategoryById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Lists all product categories.
    /// </summary>
    [HttpGet]
    [RequirePermission("product-categories:read")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ProductCategoryDto>> result = await _categoryService
            .ListAsync(cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a product category by ID.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetProductCategoryById")]
    [RequirePermission("product-categories:read")]
    [ProducesResponseType(typeof(ProductCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<ProductCategoryDto> result = await _categoryService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing product category.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("product-categories:update")]
    [ProducesResponseType(typeof(ProductCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategoryAsync(
        int id,
        [FromBody] UpdateProductCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Result<ProductCategoryDto> result = await _categoryService
            .UpdateAsync(id, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a product category if not in use.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("product-categories:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategoryAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _categoryService.DeleteAsync(id, cancellationToken);
        return ToActionResult(result);
    }
}
