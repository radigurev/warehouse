using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.GenericFiltering;
using Warehouse.Inventory.API.Interfaces.Stock;
using Warehouse.Inventory.API.Services.Base;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Services.Stock;

/// <summary>
/// Implements stock movement operations: record and search.
/// <para>See <see cref="IStockMovementService"/>.</para>
/// </summary>
public sealed class StockMovementService : BaseInventoryEntityService, IStockMovementService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public StockMovementService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task<Result<StockMovementDto>> RecordAsync(
        RecordStockMovementRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        Result? productValidation = await ValidateProductExistsAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (productValidation is not null)
            return Result<StockMovementDto>.Failure(productValidation.ErrorCode!, productValidation.ErrorMessage!, productValidation.StatusCode!.Value);

        Result? batchValidation = await ValidateBatchTrackingAsync(request.ProductId, request.BatchId, cancellationToken).ConfigureAwait(false);
        if (batchValidation is not null)
            return Result<StockMovementDto>.Failure(batchValidation.ErrorCode!, batchValidation.ErrorMessage!, batchValidation.StatusCode!.Value);

        Result? warehouseValidation = await ValidateWarehouseExistsAsync(request.WarehouseId, cancellationToken).ConfigureAwait(false);
        if (warehouseValidation is not null)
            return Result<StockMovementDto>.Failure(warehouseValidation.ErrorCode!, warehouseValidation.ErrorMessage!, warehouseValidation.StatusCode!.Value);

        Result? stockValidation = await ValidateSufficientStockAsync(request, cancellationToken).ConfigureAwait(false);
        if (stockValidation is not null)
            return Result<StockMovementDto>.Failure(stockValidation.ErrorCode!, stockValidation.ErrorMessage!, stockValidation.StatusCode!.Value);

        StockMovement movement = CreateMovementEntity(request, userId);

        Context.StockMovements.Add(movement);
        await UpdateStockLevelAsync(request, cancellationToken).ConfigureAwait(false);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        StockMovement? created = await GetMovementWithDetailsAsync(movement.Id, cancellationToken).ConfigureAwait(false);
        StockMovementDto dto = Mapper.Map<StockMovementDto>(created!);
        return Result<StockMovementDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<StockMovementDto>>> SearchAsync(
        SearchStockMovementsRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<StockMovement> query = BuildSearchQuery(request);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        List<StockMovement> items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<StockMovementDto> dtos = Mapper.Map<IReadOnlyList<StockMovementDto>>(items);

        PaginatedResponse<StockMovementDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<StockMovementDto>>.Success(response);
    }

    /// <summary>
    /// Creates the movement entity from the request.
    /// </summary>
    private static StockMovement CreateMovementEntity(RecordStockMovementRequest request, int userId)
    {
        return new StockMovement
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            LocationId = request.LocationId,
            BatchId = request.BatchId,
            Quantity = request.Quantity,
            ReasonCode = request.ReasonCode,
            ReferenceNumber = request.ReferenceNumber,
            Notes = request.Notes,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };
    }

    /// <summary>
    /// Updates or creates the stock level record for the movement.
    /// </summary>
    private async Task UpdateStockLevelAsync(
        RecordStockMovementRequest request,
        CancellationToken cancellationToken)
    {
        StockLevel? stockLevel = await Context.StockLevels
            .FirstOrDefaultAsync(s =>
                s.ProductId == request.ProductId &&
                s.WarehouseId == request.WarehouseId &&
                s.LocationId == request.LocationId &&
                s.BatchId == request.BatchId,
                cancellationToken)
            .ConfigureAwait(false);

        if (stockLevel is null)
        {
            stockLevel = new StockLevel
            {
                ProductId = request.ProductId,
                WarehouseId = request.WarehouseId,
                LocationId = request.LocationId,
                BatchId = request.BatchId,
                QuantityOnHand = request.Quantity,
                QuantityReserved = 0
            };
            Context.StockLevels.Add(stockLevel);
        }
        else
        {
            stockLevel.QuantityOnHand += request.Quantity;
            stockLevel.ModifiedAtUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Loads a movement with product, warehouse, and location details.
    /// </summary>
    private async Task<StockMovement?> GetMovementWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.StockMovements
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .Include(m => m.Location)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the search query with optional filters.
    /// </summary>
    private IQueryable<StockMovement> BuildSearchQuery(SearchStockMovementsRequest request)
    {
        IQueryable<StockMovement> query = Context.StockMovements
            .AsNoTracking()
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .Include(m => m.Location);

        if (request.ProductId.HasValue)
            query = query.Where(m => m.ProductId == request.ProductId.Value);

        if (request.WarehouseId.HasValue)
            query = query.Where(m => m.WarehouseId == request.WarehouseId.Value);

        if (request.ReasonCode.HasValue)
            query = query.Where(m => m.ReasonCode == request.ReasonCode.Value);

        if (request.DateFrom.HasValue)
            query = query.Where(m => m.CreatedAtUtc >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(m => m.CreatedAtUtc <= request.DateTo.Value);

        query = query.ApplyFilter(request.Filter);

        return query;
    }

    /// <summary>
    /// Applies sorting to the query based on the sort field.
    /// </summary>
    private static IQueryable<StockMovement> ApplySorting(
        IQueryable<StockMovement> query,
        string? sortBy,
        bool sortDescending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "quantity" => sortDescending ? query.OrderByDescending(m => m.Quantity) : query.OrderBy(m => m.Quantity),
            _ => sortDescending ? query.OrderByDescending(m => m.CreatedAtUtc) : query.OrderBy(m => m.CreatedAtUtc)
        };
    }

    /// <summary>
    /// Validates that sufficient stock exists for an outbound movement.
    /// </summary>
    private async Task<Result?> ValidateSufficientStockAsync(
        RecordStockMovementRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Quantity >= 0)
            return null;

        StockLevel? stockLevel = await Context.StockLevels
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.ProductId == request.ProductId &&
                s.WarehouseId == request.WarehouseId &&
                s.LocationId == request.LocationId &&
                s.BatchId == request.BatchId,
                cancellationToken)
            .ConfigureAwait(false);

        decimal available = stockLevel?.QuantityOnHand ?? 0;

        if (available < Math.Abs(request.Quantity))
            return Result.Failure("INSUFFICIENT_STOCK", "Insufficient stock for this movement.", 409);

        return null;
    }

    /// <summary>
    /// Validates that a product requiring batch tracking has a BatchId provided.
    /// </summary>
    private async Task<Result?> ValidateBatchTrackingAsync(
        int productId,
        int? batchId,
        CancellationToken cancellationToken)
    {
        bool requiresBatch = await Context.Products
            .AsNoTracking()
            .Where(p => p.Id == productId && !p.IsDeleted)
            .Select(p => p.RequiresBatchTracking)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (requiresBatch && batchId is null)
            return Result.Failure("BATCH_REQUIRED", "This product requires batch tracking. A BatchId must be provided.", 400);

        return null;
    }

    /// <summary>
    /// Validates that a product exists and is not deleted.
    /// </summary>
    private async Task<Result?> ValidateProductExistsAsync(int productId, CancellationToken cancellationToken)
    {
        bool exists = await Context.Products
            .AnyAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? null
            : Result.Failure("INVALID_PRODUCT", "The specified product does not exist.", 400);
    }

    /// <summary>
    /// Validates that a warehouse exists and is not deleted.
    /// </summary>
    private async Task<Result?> ValidateWarehouseExistsAsync(int warehouseId, CancellationToken cancellationToken)
    {
        bool exists = await Context.Warehouses
            .AnyAsync(w => w.Id == warehouseId && !w.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? null
            : Result.Failure("INVALID_WAREHOUSE", "The specified warehouse does not exist.", 400);
    }
}
