using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Nomenclature;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Interfaces;

/// <summary>
/// Defines state/province operations: CRUD with parent validation, soft-delete, and cascade deactivation.
/// <para>See <see cref="StateProvinceDto"/>.</para>
/// </summary>
public interface IStateProvinceService
{
    /// <summary>
    /// Creates a new state/province within an active country.
    /// </summary>
    Task<Result<StateProvinceDto>> CreateAsync(CreateStateProvinceRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a state/province by ID.
    /// </summary>
    Task<Result<StateProvinceDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Lists state/provinces for a given country, optionally including inactive ones.
    /// </summary>
    Task<Result<IReadOnlyList<StateProvinceDto>>> ListByCountryAsync(int countryId, bool includeInactive, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing state/province.
    /// </summary>
    Task<Result<StateProvinceDto>> UpdateAsync(int id, UpdateStateProvinceRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deactivates a state/province and cascades deactivation to its cities.
    /// </summary>
    Task<Result<StateProvinceDto>> DeactivateAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Reactivates an inactive state/province if its parent country is active.
    /// </summary>
    Task<Result<StateProvinceDto>> ReactivateAsync(int id, CancellationToken cancellationToken);
}
