using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Interfaces.Products;

/// <summary>
/// Defines unit of measure management operations.
/// <para>See <see cref="UnitOfMeasureDto"/>.</para>
/// </summary>
public interface IUnitOfMeasureService
{
    /// <summary>
    /// Gets a unit of measure by ID.
    /// </summary>
    Task<Result<UnitOfMeasureDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a paginated list of units of measure.
    /// </summary>
    Task<Result<PaginatedResponse<UnitOfMeasureDto>>> ListAsync(PaginationParams pagination, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new unit of measure.
    /// </summary>
    Task<Result<UnitOfMeasureDto>> CreateAsync(CreateUnitOfMeasureRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing unit of measure.
    /// </summary>
    Task<Result<UnitOfMeasureDto>> UpdateAsync(int id, UpdateUnitOfMeasureRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a unit of measure if not in use.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
