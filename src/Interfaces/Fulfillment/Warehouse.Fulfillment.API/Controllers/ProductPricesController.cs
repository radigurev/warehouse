using System.Globalization;
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
/// Handles CRUD and diagnostic price resolution operations for the Fulfillment Product Price Catalog.
/// <para>Conforms to CHG-FEAT-007 §2.2 Catalog CRUD and §2.5 Diagnostic Resolver Endpoint.</para>
/// <para>See <see cref="IProductPriceService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/product-prices")]
[Authorize]
public sealed class ProductPricesController : BaseApiController
{
    private readonly IProductPriceService _service;

    /// <summary>
    /// Initializes a new instance with the specified product price service.
    /// </summary>
    public ProductPricesController(IProductPriceService service)
    {
        _service = service;
    }

    /// <summary>Lists product prices with filters and pagination.</summary>
    [HttpGet]
    [RequirePermission("product-prices:read")]
    [ProducesResponseType(typeof(PaginatedResponse<ProductPriceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] SearchProductPricesRequest request,
        CancellationToken cancellationToken)
    {
        Result<PaginatedResponse<ProductPriceDto>> result = await _service.SearchAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Diagnostic resolver endpoint returning the effective price for the given tuple.</summary>
    [HttpGet("resolve")]
    [RequirePermission("product-prices:read")]
    [ProducesResponseType(typeof(ProductPriceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveAsync(
        [FromQuery] int productId,
        [FromQuery] string currencyCode,
        [FromQuery] string? onDate,
        CancellationToken cancellationToken)
    {
        DateTime? effectiveOnDate = null;
        if (!string.IsNullOrWhiteSpace(onDate))
        {
            if (!DateTime.TryParse(
                    onDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTime parsed))
            {
                return ToProblemResult(
                    "FULF_PRICE_INVALID_DATE",
                    "The onDate query parameter must be a valid ISO 8601 UTC datetime.",
                    400,
                    new Dictionary<string, object?> { ["onDate"] = onDate });
            }

            effectiveOnDate = parsed;
        }

        Result<ProductPriceDto> result = await _service.ResolveAsync(productId, currencyCode, effectiveOnDate, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Gets a single product price by ID.</summary>
    [HttpGet("{id:int}", Name = "GetProductPriceById")]
    [RequirePermission("product-prices:read")]
    [ProducesResponseType(typeof(ProductPriceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<ProductPriceDto> result = await _service.GetByIdAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Creates a new product price entry.</summary>
    [HttpPost]
    [RequirePermission("product-prices:create")]
    [ProducesResponseType(typeof(ProductPriceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateProductPriceRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();
        Result<ProductPriceDto> result = await _service.CreateAsync(request, userId, cancellationToken);
        return ToCreatedResult(result, "GetProductPriceById", dto => new { id = dto.Id });
    }

    /// <summary>Updates an existing product price (UnitPrice and validity window only).</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("product-prices:update")]
    [ProducesResponseType(typeof(ProductPriceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(
        int id,
        [FromBody] UpdateProductPriceRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();
        Result<ProductPriceDto> result = await _service.UpdateAsync(id, request, userId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Hard-deletes a product price; historical SO line snapshots are preserved.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("product-prices:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _service.DeleteAsync(id, cancellationToken);
        return ToActionResult(result);
    }
}
