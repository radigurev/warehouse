using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.Inventory.API.Interfaces;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Controllers;

/// <summary>
/// Handles product lifecycle operations: create, search, get, update, deactivate, and reactivate.
/// <para>See <see cref="IProductService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
[Authorize]
public sealed class ProductsController : BaseApiController
{
    private readonly IProductService _productService;

    /// <summary>
    /// Initializes a new instance with the specified product service.
    /// </summary>
    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    [HttpPost]
    [RequirePermission("products:create")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProductAsync(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<ProductDetailDto> result = await _productService
            .CreateAsync(request, userId, cancellationToken);

        return ToCreatedResult(result, "GetProductById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Searches products with filters and pagination.
    /// </summary>
    [HttpGet]
    [RequirePermission("products:read")]
    [ProducesResponseType(typeof(PaginatedResponse<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchProductsAsync(
        [FromQuery] SearchProductsRequest request,
        CancellationToken cancellationToken)
    {
        Result<PaginatedResponse<ProductDto>> result = await _productService
            .SearchAsync(request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a product by ID with details.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetProductById")]
    [RequirePermission("products:read")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<ProductDetailDto> result = await _productService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing product's fields.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("products:update")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProductAsync(
        int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<ProductDetailDto> result = await _productService
            .UpdateAsync(id, request, userId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Soft-deletes (deactivates) a product.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("products:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateProductAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _productService.DeactivateAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Reactivates a soft-deleted product.
    /// </summary>
    [HttpPost("{id:int}/reactivate")]
    [RequirePermission("products:update")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateProductAsync(int id, CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<ProductDetailDto> result = await _productService
            .ReactivateAsync(id, userId, cancellationToken);

        return ToActionResult(result);
    }
}
