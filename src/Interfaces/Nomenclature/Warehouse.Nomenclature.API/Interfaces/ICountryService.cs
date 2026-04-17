using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Nomenclature;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Interfaces;

/// <summary>
/// Defines country operations: CRUD with ISO code uniqueness, soft-delete, and cascade deactivation.
/// <para>See <see cref="CountryDto"/>, <see cref="CountryDetailDto"/>.</para>
/// </summary>
public interface ICountryService
{
    /// <summary>
    /// Creates a new country with unique ISO codes.
    /// </summary>
    Task<Result<CountryDto>> CreateAsync(CreateCountryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a country by ID with its state/provinces.
    /// </summary>
    Task<Result<CountryDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all countries, optionally including inactive ones.
    /// </summary>
    Task<Result<IReadOnlyList<CountryDto>>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing country.
    /// </summary>
    Task<Result<CountryDto>> UpdateAsync(int id, UpdateCountryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deactivates a country and cascades deactivation to its state/provinces and cities.
    /// </summary>
    Task<Result<CountryDto>> DeactivateAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Reactivates an inactive country without reactivating children.
    /// </summary>
    Task<Result<CountryDto>> ReactivateAsync(int id, CancellationToken cancellationToken);
}
