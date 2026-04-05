using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Interfaces;

/// <summary>
/// Defines zone lifecycle operations: CRUD and search.
/// <para>See <see cref="ZoneDto"/>, <see cref="ZoneDetailDto"/>.</para>
/// </summary>
public interface IZoneService
{
    /// <summary>
    /// Gets a zone by ID with storage location details.
    /// </summary>
    Task<Result<ZoneDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches zones with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<ZoneDto>>> SearchAsync(SearchZonesRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new zone within a warehouse.
    /// </summary>
    Task<Result<ZoneDetailDto>> CreateAsync(CreateZoneRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing zone.
    /// </summary>
    Task<Result<ZoneDetailDto>> UpdateAsync(int id, UpdateZoneRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a zone by ID.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
