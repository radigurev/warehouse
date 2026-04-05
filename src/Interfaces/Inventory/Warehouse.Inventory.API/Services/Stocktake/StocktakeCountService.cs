using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Interfaces.Stocktake;
using Warehouse.Inventory.API.Services.Base;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Services.Stocktake;

/// <summary>
/// Implements stocktake count operations: add, update, delete, and list.
/// <para>See <see cref="IStocktakeCountService"/>.</para>
/// </summary>
public sealed class StocktakeCountService : BaseInventoryEntityService, IStocktakeCountService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public StocktakeCountService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task<Result<StocktakeCountDto>> AddAsync(
        int sessionId,
        RecordStocktakeCountRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        StocktakeSession? session = await Context.StocktakeSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            return Result<StocktakeCountDto>.Failure("SESSION_NOT_FOUND", "Stocktake session not found.", 404);

        if (session.Status != "InProgress")
            return Result<StocktakeCountDto>.Failure("SESSION_NOT_IN_PROGRESS", "Count entries can only be added to in-progress sessions.", 409);

        Result<StocktakeCountDto>? duplicateCheck = await CheckDuplicateAsync(sessionId, request.ProductId, request.LocationId, cancellationToken).ConfigureAwait(false);
        if (duplicateCheck is not null)
            return duplicateCheck;

        decimal expected = await GetCurrentStockAsync(
            request.ProductId, session.WarehouseId, request.LocationId, cancellationToken).ConfigureAwait(false);

        StocktakeCount count = new()
        {
            SessionId = sessionId,
            ProductId = request.ProductId,
            LocationId = request.LocationId,
            ExpectedQuantity = expected,
            ActualQuantity = request.CountedQuantity,
            Variance = request.CountedQuantity - expected,
            CountedAtUtc = DateTime.UtcNow,
            CountedByUserId = userId
        };

        Context.StocktakeCounts.Add(count);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        StocktakeCountDto dto = await MapCountWithDetailsAsync(count.Id, cancellationToken).ConfigureAwait(false);
        return Result<StocktakeCountDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<StocktakeCountDto>> UpdateAsync(
        int sessionId,
        int countId,
        UpdateStocktakeCountRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        StocktakeSession? session = await Context.StocktakeSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            return Result<StocktakeCountDto>.Failure("SESSION_NOT_FOUND", "Stocktake session not found.", 404);

        if (session.Status != "InProgress")
            return Result<StocktakeCountDto>.Failure("SESSION_NOT_IN_PROGRESS", "Count entries can only be updated in in-progress sessions.", 409);

        StocktakeCount? count = await Context.StocktakeCounts
            .FirstOrDefaultAsync(c => c.Id == countId && c.SessionId == sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (count is null)
            return Result<StocktakeCountDto>.Failure("COUNT_ENTRY_NOT_FOUND", "Stocktake count entry not found.", 404);

        count.ActualQuantity = request.CountedQuantity;
        count.Variance = request.CountedQuantity - count.ExpectedQuantity;
        count.CountedAtUtc = DateTime.UtcNow;
        count.CountedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        StocktakeCountDto dto = await MapCountWithDetailsAsync(countId, cancellationToken).ConfigureAwait(false);
        return Result<StocktakeCountDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int sessionId, int countId, CancellationToken cancellationToken)
    {
        StocktakeSession? session = await Context.StocktakeSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            return Result.Failure("SESSION_NOT_FOUND", "Stocktake session not found.", 404);

        if (session.Status != "InProgress")
            return Result.Failure("SESSION_NOT_IN_PROGRESS", "Count entries can only be deleted from in-progress sessions.", 409);

        StocktakeCount? count = await Context.StocktakeCounts
            .FirstOrDefaultAsync(c => c.Id == countId && c.SessionId == sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (count is null)
            return Result.Failure("COUNT_ENTRY_NOT_FOUND", "Stocktake count entry not found.", 404);

        Context.StocktakeCounts.Remove(count);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StocktakeCountDto>>> ListBySessionAsync(
        int sessionId,
        CancellationToken cancellationToken)
    {
        bool sessionExists = await Context.StocktakeSessions
            .AnyAsync(s => s.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (!sessionExists)
            return Result<IReadOnlyList<StocktakeCountDto>>.Failure("SESSION_NOT_FOUND", "Stocktake session not found.", 404);

        List<StocktakeCount> counts = await Context.StocktakeCounts
            .AsNoTracking()
            .Include(c => c.Product)
            .Include(c => c.Location)
            .Where(c => c.SessionId == sessionId)
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<StocktakeCountDto> dtos = Mapper.Map<IReadOnlyList<StocktakeCountDto>>(counts);
        return Result<IReadOnlyList<StocktakeCountDto>>.Success(dtos);
    }

    /// <summary>
    /// Checks for a duplicate count entry for the same product and location within the session.
    /// </summary>
    private async Task<Result<StocktakeCountDto>?> CheckDuplicateAsync(
        int sessionId,
        int productId,
        int? locationId,
        CancellationToken cancellationToken)
    {
        bool exists = await Context.StocktakeCounts
            .AnyAsync(c => c.SessionId == sessionId && c.ProductId == productId && c.LocationId == locationId, cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? Result<StocktakeCountDto>.Failure("DUPLICATE_COUNT_ENTRY", "A count entry for this product and location already exists in this session.", 409)
            : null;
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
    /// Loads a count entry with product and location details and maps to DTO.
    /// </summary>
    private async Task<StocktakeCountDto> MapCountWithDetailsAsync(int countId, CancellationToken cancellationToken)
    {
        StocktakeCount count = await Context.StocktakeCounts
            .AsNoTracking()
            .Include(c => c.Product)
            .Include(c => c.Location)
            .FirstAsync(c => c.Id == countId, cancellationToken)
            .ConfigureAwait(false);

        return Mapper.Map<StocktakeCountDto>(count);
    }
}
