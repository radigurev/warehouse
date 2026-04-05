using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Interfaces.Products;

/// <summary>
/// Defines product lifecycle operations: CRUD, search, soft-delete, and reactivation.
/// <para>See <see cref="ProductDetailDto"/>, <see cref="ProductDto"/>.</para>
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Gets a product by ID with category and unit of measure details.
    /// </summary>
    Task<Result<ProductDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches products with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<ProductDto>>> SearchAsync(SearchProductsRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new product.
    /// </summary>
    Task<Result<ProductDetailDto>> CreateAsync(CreateProductRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing product's fields.
    /// </summary>
    Task<Result<ProductDetailDto>> UpdateAsync(int id, UpdateProductRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a product.
    /// </summary>
    Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Reactivates a soft-deleted product.
    /// </summary>
    Task<Result<ProductDetailDto>> ReactivateAsync(int id, int userId, CancellationToken cancellationToken);
}
