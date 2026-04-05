using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Interfaces;

/// <summary>
/// Defines product substitute link operations: create, delete, and list.
/// <para>See <see cref="ProductSubstituteDto"/>.</para>
/// </summary>
public interface IProductSubstituteService
{
    /// <summary>
    /// Lists all substitutes for a product.
    /// </summary>
    Task<Result<IReadOnlyList<ProductSubstituteDto>>> GetByProductIdAsync(int productId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new substitute link.
    /// </summary>
    Task<Result<ProductSubstituteDto>> CreateAsync(CreateProductSubstituteRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a substitute link.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
