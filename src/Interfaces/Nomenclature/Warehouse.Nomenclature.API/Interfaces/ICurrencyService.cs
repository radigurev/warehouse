using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Nomenclature;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Interfaces;

/// <summary>
/// Defines currency operations: CRUD with ISO code uniqueness and soft-delete.
/// <para>See <see cref="CurrencyDto"/>.</para>
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Creates a new currency with a unique ISO 4217 code.
    /// </summary>
    Task<Result<CurrencyDto>> CreateAsync(CreateCurrencyRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a currency by ID.
    /// </summary>
    Task<Result<CurrencyDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all currencies, optionally including inactive ones.
    /// </summary>
    Task<Result<IReadOnlyList<CurrencyDto>>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing currency.
    /// </summary>
    Task<Result<CurrencyDto>> UpdateAsync(int id, UpdateCurrencyRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deactivates a currency.
    /// </summary>
    Task<Result<CurrencyDto>> DeactivateAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Reactivates an inactive currency.
    /// </summary>
    Task<Result<CurrencyDto>> ReactivateAsync(int id, CancellationToken cancellationToken);
}
