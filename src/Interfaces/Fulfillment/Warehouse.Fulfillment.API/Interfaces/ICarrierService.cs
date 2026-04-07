using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Interfaces;

/// <summary>
/// Defines carrier management operations: CRUD, service levels, deactivation.
/// <para>See <see cref="CarrierDetailDto"/>, <see cref="CarrierDto"/>.</para>
/// </summary>
public interface ICarrierService
{
    /// <summary>Creates a new carrier.</summary>
    Task<Result<CarrierDetailDto>> CreateAsync(CreateCarrierRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>Gets a carrier by ID with service levels.</summary>
    Task<Result<CarrierDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Searches carriers with filters and pagination.</summary>
    Task<Result<PaginatedResponse<CarrierDto>>> SearchAsync(SearchCarriersRequest request, CancellationToken cancellationToken);

    /// <summary>Updates carrier fields.</summary>
    Task<Result<CarrierDetailDto>> UpdateAsync(int id, UpdateCarrierRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>Deactivates a carrier.</summary>
    Task<Result<CarrierDetailDto>> DeactivateAsync(int id, int userId, CancellationToken cancellationToken);

    /// <summary>Creates a service level for a carrier.</summary>
    Task<Result<CarrierServiceLevelDto>> CreateServiceLevelAsync(int carrierId, CreateCarrierServiceLevelRequest request, CancellationToken cancellationToken);

    /// <summary>Lists service levels for a carrier.</summary>
    Task<Result<IReadOnlyList<CarrierServiceLevelDto>>> ListServiceLevelsAsync(int carrierId, CancellationToken cancellationToken);

    /// <summary>Updates a carrier service level.</summary>
    Task<Result<CarrierServiceLevelDto>> UpdateServiceLevelAsync(int carrierId, int levelId, UpdateCarrierServiceLevelRequest request, CancellationToken cancellationToken);

    /// <summary>Deletes a carrier service level.</summary>
    Task<Result> DeleteServiceLevelAsync(int carrierId, int levelId, CancellationToken cancellationToken);
}
