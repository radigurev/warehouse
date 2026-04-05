using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.GenericFiltering;
using Warehouse.Inventory.API.Interfaces;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Services;

/// <summary>
/// Implements stocktake session operations: create, start, count, finalize, cancel, get, and search.
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
            return Result<StocktakeSessionDetailDto>.Failure("INVALID_STATUS", "Only draft sessions can be started.", 409);

        session.Status = "InProgress";
        session.StartedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        StocktakeSessionDetailDto dto = Mapper.Map<StocktakeSessionDetailDto>(session);
        return Result<StocktakeSessionDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<StocktakeSessionDetailDto>> RecordCountAsync(
        int sessionId,
        RecordStocktakeCountRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        StocktakeSession? session = await GetSessionWithDetailsAsync(sessionId, cancellationToken).ConfigureAwait(false);

        if (session is null)
            return Result<StocktakeSessionDetailDto>.Failure("SESSION_NOT_FOUND", "Stocktake session not found.", 404);

        if (session.Status != "InProgress")
            return Result<StocktakeSessionDetailDto>.Failure("INVALID_STATUS", "Counts can only be recorded for in-progress sessions.", 409);

        decimal expected = await GetCurrentStockAsync(
            request.ProductId, session.WarehouseId, request.LocationId, cancellationToken).ConfigureAwait(false);

        StocktakeCount? existingCount = session.Counts
            .FirstOrDefault(c => c.ProductId == request.ProductId && c.LocationId == request.LocationId);

        if (existingCount is not null)
        {
            existingCount.ActualQuantity = request.CountedQuantity;
            existingCount.ExpectedQuantity = expected;
            existingCount.Variance = request.CountedQuantity - expected;
            existingCount.CountedAtUtc = DateTime.UtcNow;
            existingCount.CountedByUserId = userId;
        }
        else
        {
            Context.StocktakeCounts.Add(new StocktakeCount
            {
                SessionId = sessionId,
                ProductId = request.ProductId,
                LocationId = request.LocationId,
                ExpectedQuantity = expected,
                ActualQuantity = request.CountedQuantity,
                Variance = request.CountedQuantity - expected,
                CountedAtUtc = DateTime.UtcNow,
                CountedByUserId = userId
            });
        }

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        StocktakeSession? updated = await GetSessionWithDetailsAsync(sessionId, cancellationToken).ConfigureAwait(false);
        StocktakeSessionDetailDto dto = Mapper.Map<StocktakeSessionDetailDto>(updated!);
        return Result<StocktakeSessionDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<StocktakeSessionDetailDto>> FinalizeAsync(
        int id,
        int userId,
        CancellationToken cancellationToken)
    {
        StocktakeSession? session = await GetSessionWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (session is null)
            return Result<StocktakeSessionDetailDto>.Failure("SESSION_NOT_FOUND", "Stocktake session not found.", 404);

        if (session.Status != "InProgress")
            return Result<StocktakeSessionDetailDto>.Failure("INVALID_STATUS", "Only in-progress sessions can be finalized.", 409);

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
            return Result.Failure("INVALID_STATUS", "Completed or cancelled sessions cannot be cancelled.", 409);

        session.Status = "Cancelled";
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// Gets the current on-hand stock for a product at a location.
    /// </summary>
    private async Task<decimal> GetCurrentStockAsync(
        int productId,
        int warehouseId,
        int? locationId,
        CancellationToken cancellationToken)
    {
        StockLevel? stockLevel = await Context.StockLevels
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId &&
                s.LocationId == locationId,
                cancellationToken)
            .ConfigureAwait(false);

        return stockLevel?.QuantityOnHand ?? 0;
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
