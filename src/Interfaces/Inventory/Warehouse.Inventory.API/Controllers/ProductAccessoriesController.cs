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
/// Handles product accessory link operations: create, list, and delete.
/// <para>See <see cref="IProductAccessoryService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/product-accessories")]
[Authorize]
public sealed class ProductAccessoriesController : BaseApiController
{
    private readonly IProductAccessoryService _accessoryService;

    /// <summary>
    /// Initializes a new instance with the specified accessory service.
    /// </summary>
    public ProductAccessoriesController(IProductAccessoryService accessoryService)
    {
        _accessoryService = accessoryService;
    }

    /// <summary>
    /// Creates a new product accessory link.
    /// </summary>
    [HttpPost]
    [RequirePermission("product-accessories:create")]
    [ProducesResponseType(typeof(ProductAccessoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAccessoryAsync(
        [FromBody] CreateProductAccessoryRequest request,
        CancellationToken cancellationToken)
    {
        Result<ProductAccessoryDto> result = await _accessoryService
            .CreateAsync(request, cancellationToken);

        return ToCreatedResult(result, "GetAccessoriesByProduct", dto => new { productId = dto.ProductId });
    }

    /// <summary>
    /// Lists all accessories for a product.
    /// </summary>
    [HttpGet("by-product/{productId:int}", Name = "GetAccessoriesByProduct")]
    [RequirePermission("product-accessories:read")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductAccessoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProductAsync(int productId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ProductAccessoryDto>> result = await _accessoryService
            .GetByProductIdAsync(productId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a product accessory link.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("product-accessories:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccessoryAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _accessoryService.DeleteAsync(id, cancellationToken);
        return ToActionResult(result);
    }
}
