using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Nomenclature;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Interfaces;

/// <summary>
/// Defines city operations: CRUD with parent validation and soft-delete.
/// <para>See <see cref="CityDto"/>.</para>
/// </summary>
public interface ICityService
{
    /// <summary>
    /// Creates a new city within an active state/province.
    /// </summary>
    Task<Result<CityDto>> CreateAsync(CreateCityRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a city by ID.
    /// </summary>
    Task<Result<CityDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Lists cities for a given state/province, optionally including inactive ones.
    /// </summary>
    Task<Result<IReadOnlyList<CityDto>>> ListByStateProvinceAsync(int stateProvinceId, bool includeInactive, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing city.
    /// </summary>
    Task<Result<CityDto>> UpdateAsync(int id, UpdateCityRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deactivates a city.
    /// </summary>
    Task<Result<CityDto>> DeactivateAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Reactivates an inactive city if its parent state/province is active.
    /// </summary>
    Task<Result<CityDto>> ReactivateAsync(int id, CancellationToken cancellationToken);
}
