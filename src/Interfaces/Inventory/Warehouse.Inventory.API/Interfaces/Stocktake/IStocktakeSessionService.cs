using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Interfaces;

/// <summary>
/// Defines stocktake session operations: create, start, count, finalize, cancel, get, and search.
/// <para>See <see cref="StocktakeSessionDto"/>, <see cref="StocktakeSessionDetailDto"/>.</para>
/// </summary>
public interface IStocktakeSessionService
{
    /// <summary>
    /// Gets a session by ID with counts.
    /// </summary>
    Task<Result<StocktakeSessionDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches sessions with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<StocktakeSessionDto>>> SearchAsync(SearchStocktakeSessionsRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new session in Draft status.
    /// </summary>
    Task<Result<StocktakeSessionDetailDto>> CreateAsync(CreateStocktakeSessionRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Transitions a session from Draft to InProgress.
    /// </summary>
    Task<Result<StocktakeSessionDetailDto>> StartAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Records a count entry for an in-progress session.
    /// </summary>
    Task<Result<StocktakeSessionDetailDto>> RecordCountAsync(int sessionId, RecordStocktakeCountRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Finalizes an in-progress session as Completed.
    /// </summary>
    Task<Result<StocktakeSessionDetailDto>> FinalizeAsync(int id, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a draft or in-progress session.
    /// </summary>
    Task<Result> CancelAsync(int id, CancellationToken cancellationToken);
}
