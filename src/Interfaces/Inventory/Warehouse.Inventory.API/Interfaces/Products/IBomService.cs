using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Interfaces.Products;

/// <summary>
/// Defines bill of materials operations: CRUD and line management.
/// <para>See <see cref="BomDto"/>, <see cref="BomLineDto"/>.</para>
/// </summary>
public interface IBomService
{
    /// <summary>
    /// Gets a BOM by ID with lines.
    /// </summary>
    Task<Result<BomDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all BOMs for a product.
    /// </summary>
    Task<Result<IReadOnlyList<BomDto>>> GetByProductIdAsync(int productId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new BOM with lines.
    /// </summary>
    Task<Result<BomDto>> CreateAsync(CreateBomRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates a BOM header.
    /// </summary>
    Task<Result<BomDto>> UpdateAsync(int id, UpdateBomRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a component line to an existing BOM.
    /// </summary>
    Task<Result<BomDto>> AddLineAsync(int bomId, AddBomLineRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a component line from a BOM.
    /// </summary>
    Task<Result> RemoveLineAsync(int bomId, int lineId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a BOM and all its lines.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
