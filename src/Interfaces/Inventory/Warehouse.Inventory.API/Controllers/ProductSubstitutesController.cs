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
/// Handles product substitute link operations: create, list, and delete.
/// <para>See <see cref="IProductSubstituteService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/product-substitutes")]
[Authorize]
public sealed class ProductSubstitutesController : BaseApiController
{
    private readonly IProductSubstituteService _substituteService;

    /// <summary>
    /// Initializes a new instance with the specified substitute service.
    /// </summary>
    public ProductSubstitutesController(IProductSubstituteService substituteService)
    {
        _substituteService = substituteService;
    }

    /// <summary>
    /// Creates a new product substitute link.
    /// </summary>
    [HttpPost]
    [RequirePermission("product-substitutes:create")]
    [ProducesResponseType(typeof(ProductSubstituteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSubstituteAsync(
        [FromBody] CreateProductSubstituteRequest request,
        CancellationToken cancellationToken)
    {
        Result<ProductSubstituteDto> result = await _substituteService
            .CreateAsync(request, cancellationToken);

        return ToCreatedResult(result, "GetSubstitutesByProduct", dto => new { productId = dto.ProductId });
    }

    /// <summary>
    /// Lists all substitutes for a product.
    /// </summary>
    [HttpGet("by-product/{productId:int}", Name = "GetSubstitutesByProduct")]
    [RequirePermission("product-substitutes:read")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductSubstituteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProductAsync(int productId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ProductSubstituteDto>> result = await _substituteService
            .GetByProductIdAsync(productId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a product substitute link.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("product-substitutes:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubstituteAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _substituteService.DeleteAsync(id, cancellationToken);
        return ToActionResult(result);
    }
}
