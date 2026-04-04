using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Customers.API.Authorization;
using Warehouse.Customers.API.Interfaces;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Controllers;

/// <summary>
/// Handles customer category operations: create, list, get, update, and delete.
/// <para>See <see cref="ICustomerCategoryService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customer-categories")]
[Authorize]
public sealed class CustomerCategoriesController : BaseCustomersController
{
    private readonly ICustomerCategoryService _categoryService;

    /// <summary>
    /// Initializes a new instance with the specified customer category service.
    /// </summary>
    public CustomerCategoriesController(ICustomerCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Creates a new customer category with a unique name.
    /// </summary>
    [HttpPost]
    [RequirePermission("customers:write")]
    [ProducesResponseType(typeof(CustomerCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategoryAsync(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Result<CustomerCategoryDto> result = await _categoryService
            .CreateAsync(request, cancellationToken);

        return ToCreatedResult(result, "GetCategoryById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Lists all customer categories.
    /// </summary>
    [HttpGet]
    [RequirePermission("customers:read")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CustomerCategoryDto>> result = await _categoryService
            .GetAllAsync(cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a customer category by ID.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetCategoryById")]
    [RequirePermission("customers:read")]
    [ProducesResponseType(typeof(CustomerCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<CustomerCategoryDto> result = await _categoryService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing customer category.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(typeof(CustomerCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategoryAsync(
        int id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Result<CustomerCategoryDto> result = await _categoryService
            .UpdateAsync(id, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a category if no customers are assigned to it.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("customers:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategoryAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _categoryService.DeleteAsync(id, cancellationToken);
        return ToActionResult(result);
    }
}
