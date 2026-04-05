using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Interfaces.Stocktake;

/// <summary>
/// Defines stocktake count operations: add, update, delete, and list.
/// <para>See <see cref="StocktakeCountDto"/>, <see cref="RecordStocktakeCountRequest"/>.</para>
/// </summary>
public interface IStocktakeCountService
{
    /// <summary>
    /// Adds a count entry to an in-progress session.
    /// </summary>
    Task<Result<StocktakeCountDto>> AddAsync(int sessionId, RecordStocktakeCountRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the actual quantity on an existing count entry.
    /// </summary>
    Task<Result<StocktakeCountDto>> UpdateAsync(int sessionId, int countId, UpdateStocktakeCountRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a count entry from an in-progress session.
    /// </summary>
    Task<Result> DeleteAsync(int sessionId, int countId, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all count entries for a stocktake session.
    /// </summary>
    Task<Result<IReadOnlyList<StocktakeCountDto>>> ListBySessionAsync(int sessionId, CancellationToken cancellationToken);
}
