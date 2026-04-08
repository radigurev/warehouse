using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.GenericFiltering;
using Warehouse.Inventory.API.Interfaces.Stocktake;
using Warehouse.Inventory.API.Services.Base;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Services.Stocktake;

/// <summary>
/// Implements stocktake session lifecycle operations: create, start, complete, cancel, get, and search.
/// <para>See <see cref="IStocktakeSessionService"/>.</para>
/// </summary>
public sealed class StocktakeSessionService : BaseInventoryEntityService, IStocktakeSessionService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public StocktakeSessionService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task<Result<StocktakeSessionDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        StocktakeSession? session = await GetSessionWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (session is null)
            return Result<StocktakeSessionDetailDto>.Failure("SESSION_NOT_FOUND", "Stocktake session not found.", 404);

        StocktakeSessionDetailDto dto = Mapper.Map<StocktakeSessionDetailDto>(session);
        return Result<StocktakeSessionDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<StocktakeSessionDto>>> SearchAsync(
        SearchStocktakeSessionsRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<StocktakeSession> query = BuildSearchQuery(request);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<StocktakeSession> items = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<StocktakeSessionDto> dtos = Mapper.Map<IReadOnlyList<StocktakeSessionDto>>(items);

        PaginatedResponse<StocktakeSessionDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<StocktakeSessionDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<StocktakeSessionDetailDto>> CreateAsync(
        CreateStocktakeSessionRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        StocktakeSession session = new()
        {
            WarehouseId = request.WarehouseId,
            ZoneId = request.ZoneId,
            Name = request.Name,
            Notes = request.Notes,
            Status = "Draft",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        Context.StocktakeSessions.Add(session);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        StocktakeSession? created = await GetSessionWithDetailsAsync(session.Id, cancellationToken).ConfigureAwait(false);
        StocktakeSessionDetailDto dto = Mapper.Map<StocktakeSessionDetailDto>(created!);
        return Result<StocktakeSessionDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<StocktakeSessionDetailDto>> StartAsync(int id, CancellationToken cancellationToken)
    {
        StocktakeSession? session = await GetSessionWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (session is null)
            return Result<StocktakeSessionDetailDto>.Failure("SESSION_NOT_FOUND", "Stocktake session not found.", 404);

        if (session.Status != "Draft")
            return Result<StocktakeSessionDetailDto>.Failure("SESSION_NOT_DRAFT", "Only draft sessions can be started. Current status: " + session.Status + ".", 409);

        session.Status = "InProgress";
        session.StartedAtUtc = DateTime.UtcNow;

        await SnapshotStockLevelsAsync(session, cancellationToken).ConfigureAwait(false);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        StocktakeSession? updated = await GetSessionWithDetailsAsync(session.Id, cancellationToken).ConfigureAwait(false);
        StocktakeSessionDetailDto dto = Mapper.Map<StocktakeSessionDetailDto>(updated!);
        return Result<StocktakeSessionDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<StocktakeSessionDetailDto>> CompleteAsync(
        int id,
        int userId,
        CancellationToken cancellationToken)
    {
        StocktakeSession? session = await GetSessionWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (session is null)
            return Result<StocktakeSessionDetailDto>.Failure("SESSION_NOT_FOUND", "Stocktake session not found.", 404);

        if (session.Status != "InProgress")
            return Result<StocktakeSessionDetailDto>.Failure("SESSION_NOT_IN_PROGRESS", "Only in-progress sessions can be completed. Current status: " + session.Status + ".", 409);

        session.Status = "Completed";
        session.CompletedAtUtc = DateTime.UtcNow;
        session.CompletedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        StocktakeSessionDetailDto dto = Mapper.Map<StocktakeSessionDetailDto>(session);
        return Result<StocktakeSessionDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> CancelAsync(int id, CancellationToken cancellationToken)
    {
        StocktakeSession? session = await Context.StocktakeSessions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            return Result.Failure("SESSION_NOT_FOUND", "Stocktake session not found.", 404);

        if (session.Status == "Completed" || session.Status == "Cancelled")
            return Result.Failure("SESSION_CANNOT_CANCEL", "Cannot cancel a session with status: " + session.Status + ".", 409);

        session.Status = "Cancelled";
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StocktakeCountDto>>> GetVarianceReportAsync(
        int sessionId,
        CancellationToken cancellationToken)
    {
        StocktakeSession? session = await Context.StocktakeSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            return Result<IReadOnlyList<StocktakeCountDto>>.Failure("SESSION_NOT_FOUND", "Stocktake session not found.", 404);

        if (session.Status != "Completed")
            return Result<IReadOnlyList<StocktakeCountDto>>.Failure("SESSION_NOT_COMPLETED", "Variance report is only available for completed sessions.", 409);

        List<StocktakeCount> varianceCounts = await Context.StocktakeCounts
            .AsNoTracking()
            .Include(c => c.Product)
            .Include(c => c.Location)
            .Where(c => c.SessionId == sessionId && c.Variance != 0)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<StocktakeCountDto> dtos = Mapper.Map<IReadOnlyList<StocktakeCountDto>>(varianceCounts);
        return Result<IReadOnlyList<StocktakeCountDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<InventoryAdjustmentDetailDto>> CreateAdjustmentFromSessionAsync(
        int sessionId,
        int userId,
        CancellationToken cancellationToken)
    {
        StocktakeSession? session = await GetSessionWithDetailsAsync(sessionId, cancellationToken).ConfigureAwait(false);

        if (session is null)
            return Result<InventoryAdjustmentDetailDto>.Failure("SESSION_NOT_FOUND", "Stocktake session not found.", 404);

        if (session.Status != "Completed")
            return Result<InventoryAdjustmentDetailDto>.Failure("SESSION_NOT_COMPLETED", "Adjustments can only be created from completed sessions.", 409);

        List<StocktakeCount> varianceCounts = session.Counts
            .Where(c => c.Variance != 0)
            .ToList();

        if (varianceCounts.Count == 0)
            return Result<InventoryAdjustmentDetailDto>.Failure("NO_VARIANCE", "No variances found to create an adjustment.", 400);

        InventoryAdjustment adjustment = CreateAdjustmentFromCounts(session, varianceCounts, userId);

        Context.InventoryAdjustments.Add(adjustment);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        InventoryAdjustment? created = await LoadAdjustmentWithDetailsAsync(adjustment.Id, cancellationToken).ConfigureAwait(false);
        InventoryAdjustmentDetailDto dto = Mapper.Map<InventoryAdjustmentDetailDto>(created!);
        return Result<InventoryAdjustmentDetailDto>.Success(dto);
    }

    /// <summary>
    /// Creates an adjustment entity from stocktake count variances.
    /// </summary>
    private static InventoryAdjustment CreateAdjustmentFromCounts(
        StocktakeSession session,
        List<StocktakeCount> varianceCounts,
        int userId)
    {
        InventoryAdjustment adjustment = new()
        {
            Reason = "Stocktake variance",
            Notes = $"Auto-generated from stocktake session #{session.Id}: {session.Name}",
            Status = "Pending",
            SourceStocktakeSessionId = session.Id,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        foreach (StocktakeCount count in varianceCounts)
        {
            adjustment.Lines.Add(new InventoryAdjustmentLine
            {
                ProductId = count.ProductId,
                WarehouseId = session.WarehouseId,
                LocationId = count.LocationId,
                ExpectedQuantity = count.ExpectedQuantity,
                ActualQuantity = count.ActualQuantity
            });
        }

        return adjustment;
    }

    /// <summary>
    /// Snapshots current stock levels into count records for the session.
    /// </summary>
    private async Task SnapshotStockLevelsAsync(
        StocktakeSession session,
        CancellationToken cancellationToken)
    {
        IQueryable<StockLevel> query = Context.StockLevels
            .Where(s => s.WarehouseId == session.WarehouseId);

        if (session.ZoneId.HasValue)
        {
            query = query.Where(s =>
                s.LocationId != null &&
                s.Location != null &&
                s.Location.ZoneId == session.ZoneId.Value);
        }

        List<StockLevel> stockLevels = await query
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (StockLevel stockLevel in stockLevels)
        {
            Context.StocktakeCounts.Add(new StocktakeCount
            {
                SessionId = session.Id,
                ProductId = stockLevel.ProductId,
                LocationId = stockLevel.LocationId,
                ExpectedQuantity = stockLevel.QuantityOnHand,
                ActualQuantity = 0,
                Variance = -stockLevel.QuantityOnHand
            });
        }
    }

    /// <summary>
    /// Loads an adjustment with lines including product and location details.
    /// </summary>
    private async Task<InventoryAdjustment?> LoadAdjustmentWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.InventoryAdjustments
            .Include(a => a.Lines).ThenInclude(l => l.Product)
            .Include(a => a.Lines).ThenInclude(l => l.Location)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a session with warehouse, zone, and count details.
    /// </summary>
    private async Task<StocktakeSession?> GetSessionWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.StocktakeSessions
            .Include(s => s.Warehouse)
            .Include(s => s.Zone)
            .Include(s => s.Counts).ThenInclude(c => c.Product)
            .Include(s => s.Counts).ThenInclude(c => c.Location)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the search query with optional filters.
    /// </summary>
    private IQueryable<StocktakeSession> BuildSearchQuery(SearchStocktakeSessionsRequest request)
    {
        IQueryable<StocktakeSession> query = Context.StocktakeSessions
            .AsNoTracking()
            .Include(s => s.Warehouse)
            .Include(s => s.Zone);

        if (request.WarehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == request.WarehouseId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(s => s.Status == request.Status);

        if (request.DateFrom.HasValue)
            query = query.Where(s => s.CreatedAtUtc >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(s => s.CreatedAtUtc <= request.DateTo.Value);

        query = query.ApplyFilter(request.Filter);

        return query;
    }
}
