using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Interfaces.Stocktake;

/// <summary>
/// Defines stocktake session lifecycle operations: create, start, complete, cancel, get, and search.
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
    /// Completes an in-progress session, setting status to Completed.
    /// </summary>
    Task<Result<StocktakeSessionDetailDto>> CompleteAsync(int id, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a draft or in-progress session.
    /// </summary>
    Task<Result> CancelAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all count entries with non-zero variance for a completed session.
    /// </summary>
    Task<Result<IReadOnlyList<StocktakeCountDto>>> GetVarianceReportAsync(int sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an inventory adjustment from the variances of a completed session.
    /// </summary>
    Task<Result<InventoryAdjustmentDetailDto>> CreateAdjustmentFromSessionAsync(int sessionId, int userId, CancellationToken cancellationToken);
}
