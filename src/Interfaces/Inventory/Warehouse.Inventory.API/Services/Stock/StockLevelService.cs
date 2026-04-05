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
/// Implements stock level query operations: get, search, and summary.
/// <para>See <see cref="IStockLevelService"/>.</para>
/// </summary>
public sealed class StockLevelService : BaseInventoryEntityService, IStockLevelService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public StockLevelService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task<Result<StockLevelDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        StockLevel? stockLevel = await Context.StockLevels
            .AsNoTracking()
            .Include(s => s.Product)
            .Include(s => s.Warehouse)
            .Include(s => s.Location)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (stockLevel is null)
            return Result<StockLevelDto>.Failure("STOCK_LEVEL_NOT_FOUND", "Stock level not found.", 404);

        StockLevelDto dto = Mapper.Map<StockLevelDto>(stockLevel);
        return Result<StockLevelDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<StockLevelDto>>> SearchAsync(
        SearchStockLevelsRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<StockLevel> query = BuildSearchQuery(request);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        List<StockLevel> items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<StockLevelDto> dtos = Mapper.Map<IReadOnlyList<StockLevelDto>>(items);

        PaginatedResponse<StockLevelDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<StockLevelDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<StockSummaryDto>> GetSummaryByProductAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        Product? product = await Context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
            return Result<StockSummaryDto>.Failure("PRODUCT_NOT_FOUND", "Product not found.", 404);

        List<StockLevel> levels = await Context.StockLevels
            .AsNoTracking()
            .Include(s => s.Warehouse)
            .Include(s => s.Location)
            .Where(s => s.ProductId == productId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<StockLevelDto> breakdown = Mapper.Map<IReadOnlyList<StockLevelDto>>(levels);

        StockSummaryDto summary = new()
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ProductCode = product.Code,
            TotalOnHand = levels.Sum(l => l.QuantityOnHand),
            TotalReserved = levels.Sum(l => l.QuantityReserved),
            TotalAvailable = levels.Sum(l => l.QuantityOnHand - l.QuantityReserved),
            WarehouseBreakdown = breakdown
        };

        return Result<StockSummaryDto>.Success(summary);
    }

    /// <summary>
    /// Builds the search query with optional filters.
    /// </summary>
    private IQueryable<StockLevel> BuildSearchQuery(SearchStockLevelsRequest request)
    {
        IQueryable<StockLevel> query = Context.StockLevels
            .AsNoTracking()
            .Include(s => s.Product)
            .Include(s => s.Warehouse)
            .Include(s => s.Location);

        if (request.ProductId.HasValue)
            query = query.Where(s => s.ProductId == request.ProductId.Value);

        if (request.WarehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == request.WarehouseId.Value);

        if (request.LocationId.HasValue)
            query = query.Where(s => s.LocationId == request.LocationId.Value);

        if (request.MinQuantity.HasValue)
            query = query.Where(s => s.QuantityOnHand >= request.MinQuantity.Value);

        query = query.ApplyFilter(request.Filter);

        return query;
    }

    /// <summary>
    /// Applies sorting to the query based on the sort field.
    /// </summary>
    private static IQueryable<StockLevel> ApplySorting(
        IQueryable<StockLevel> query,
        string? sortBy,
        bool sortDescending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "quantity" => sortDescending ? query.OrderByDescending(s => s.QuantityOnHand) : query.OrderBy(s => s.QuantityOnHand),
            _ => sortDescending ? query.OrderByDescending(s => s.ProductId) : query.OrderBy(s => s.ProductId)
        };
    }
}
