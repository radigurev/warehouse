using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Interfaces;

/// <summary>
/// Defines product accessory link operations: create, delete, and list.
/// <para>See <see cref="ProductAccessoryDto"/>.</para>
/// </summary>
public interface IProductAccessoryService
{
    /// <summary>
    /// Lists all accessories for a product.
    /// </summary>
    Task<Result<IReadOnlyList<ProductAccessoryDto>>> GetByProductIdAsync(int productId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new accessory link.
    /// </summary>
    Task<Result<ProductAccessoryDto>> CreateAsync(CreateProductAccessoryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an accessory link.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
