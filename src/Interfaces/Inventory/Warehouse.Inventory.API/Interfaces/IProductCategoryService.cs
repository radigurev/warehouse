using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Interfaces;

/// <summary>
/// Defines product category management operations.
/// <para>See <see cref="ProductCategoryDto"/>.</para>
/// </summary>
public interface IProductCategoryService
{
    /// <summary>
    /// Gets a product category by ID.
    /// </summary>
    Task<Result<ProductCategoryDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all product categories.
    /// </summary>
    Task<Result<IReadOnlyList<ProductCategoryDto>>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new product category.
    /// </summary>
    Task<Result<ProductCategoryDto>> CreateAsync(CreateProductCategoryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing product category.
    /// </summary>
    Task<Result<ProductCategoryDto>> UpdateAsync(int id, UpdateProductCategoryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a product category if not in use.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
