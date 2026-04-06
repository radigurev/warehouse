using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.GenericFiltering;
using Warehouse.Inventory.API.Interfaces.Stock;
using Warehouse.Inventory.API.Services.Base;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Services.Stock;

/// <summary>
/// Implements inventory adjustment operations: create, approve, reject, apply, get, and search.
/// <para>See <see cref="IInventoryAdjustmentService"/>.</para>
/// </summary>
public sealed class InventoryAdjustmentService : BaseInventoryEntityService, IInventoryAdjustmentService
{
    private readonly IPublishEndpoint _publishEndpoint;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public InventoryAdjustmentService(InventoryDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint)
        : base(context, mapper)
    {
        _publishEndpoint = publishEndpoint;
    }

    /// <inheritdoc />
    public async Task<Result<InventoryAdjustmentDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        InventoryAdjustment? adjustment = await GetAdjustmentWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (adjustment is null)
            return Result<InventoryAdjustmentDetailDto>.Failure("ADJUSTMENT_NOT_FOUND", "Inventory adjustment not found.", 404);

        InventoryAdjustmentDetailDto dto = Mapper.Map<InventoryAdjustmentDetailDto>(adjustment);
        return Result<InventoryAdjustmentDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<InventoryAdjustmentDto>>> SearchAsync(
        SearchAdjustmentsRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<InventoryAdjustment> query = BuildSearchQuery(request);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<InventoryAdjustment> items = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<InventoryAdjustmentDto> dtos = Mapper.Map<IReadOnlyList<InventoryAdjustmentDto>>(items);

        PaginatedResponse<InventoryAdjustmentDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<InventoryAdjustmentDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<InventoryAdjustmentDetailDto>> CreateAsync(
        CreateAdjustmentRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        InventoryAdjustment adjustment = new()
        {
            Reason = request.Reason,
            Notes = request.Notes,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        foreach (CreateAdjustmentLineRequest lineReq in request.Lines)
        {
            decimal expected = await GetCurrentStockAsync(lineReq.ProductId, request.WarehouseId, lineReq.LocationId, cancellationToken).ConfigureAwait(false);

            adjustment.Lines.Add(new InventoryAdjustmentLine
            {
                ProductId = lineReq.ProductId,
                WarehouseId = request.WarehouseId,
                LocationId = lineReq.LocationId,
                ExpectedQuantity = expected,
                ActualQuantity = lineReq.ActualQuantity
            });
        }

        Context.InventoryAdjustments.Add(adjustment);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        InventoryAdjustment? created = await GetAdjustmentWithDetailsAsync(adjustment.Id, cancellationToken).ConfigureAwait(false);
        InventoryAdjustmentDetailDto dto = Mapper.Map<InventoryAdjustmentDetailDto>(created!);
        return Result<InventoryAdjustmentDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<InventoryAdjustmentDetailDto>> ApproveAsync(
        int id,
        ApproveAdjustmentRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        InventoryAdjustment? adjustment = await GetAdjustmentWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (adjustment is null)
            return Result<InventoryAdjustmentDetailDto>.Failure("ADJUSTMENT_NOT_FOUND", "Inventory adjustment not found.", 404);

        if (adjustment.Status != "Pending")
            return Result<InventoryAdjustmentDetailDto>.Failure("ADJUSTMENT_NOT_PENDING", "Only pending adjustments can be approved.", 409);

        adjustment.Status = "Approved";
        adjustment.ApprovedAtUtc = DateTime.UtcNow;
        adjustment.ApprovedByUserId = userId;

        if (!string.IsNullOrWhiteSpace(request.Notes))
            adjustment.Notes = $"{adjustment.Notes}\n[Approval] {request.Notes}".Trim();

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        InventoryAdjustmentDetailDto dto = Mapper.Map<InventoryAdjustmentDetailDto>(adjustment);
        return Result<InventoryAdjustmentDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<InventoryAdjustmentDetailDto>> RejectAsync(
        int id,
        RejectAdjustmentRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        InventoryAdjustment? adjustment = await GetAdjustmentWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (adjustment is null)
            return Result<InventoryAdjustmentDetailDto>.Failure("ADJUSTMENT_NOT_FOUND", "Inventory adjustment not found.", 404);

        if (adjustment.Status != "Pending")
            return Result<InventoryAdjustmentDetailDto>.Failure("ADJUSTMENT_NOT_PENDING", "Only pending adjustments can be rejected.", 409);

        adjustment.Status = "Rejected";
        adjustment.RejectedAtUtc = DateTime.UtcNow;
        adjustment.RejectedByUserId = userId;
        adjustment.RejectionReason = request.Notes;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        InventoryAdjustmentDetailDto dto = Mapper.Map<InventoryAdjustmentDetailDto>(adjustment);
        return Result<InventoryAdjustmentDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<InventoryAdjustmentDetailDto>> ApplyAsync(
        int id,
        int userId,
        CancellationToken cancellationToken)
    {
        InventoryAdjustment? adjustment = await GetAdjustmentWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (adjustment is null)
            return Result<InventoryAdjustmentDetailDto>.Failure("ADJUSTMENT_NOT_FOUND", "Inventory adjustment not found.", 404);

        if (adjustment.Status != "Approved")
            return Result<InventoryAdjustmentDetailDto>.Failure("ADJUSTMENT_NOT_APPROVED", "Only approved adjustments can be applied.", 409);

        await using IDbContextTransaction transaction = await Context.Database
            .BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        Result? stockValidation = await ApplyAdjustmentLinesToStockAsync(adjustment, userId, cancellationToken).ConfigureAwait(false);
        if (stockValidation is not null)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return Result<InventoryAdjustmentDetailDto>.Failure(stockValidation.ErrorCode!, stockValidation.ErrorMessage!, stockValidation.StatusCode!.Value);
        }

        adjustment.Status = "Applied";
        adjustment.AppliedAtUtc = DateTime.UtcNow;
        adjustment.AppliedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _publishEndpoint.Publish(new InventoryAdjustmentAppliedEvent
            {
                AdjustmentId = adjustment.Id,
                AppliedByUserId = userId,
                AppliedAt = adjustment.AppliedAtUtc!.Value
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
        }

        InventoryAdjustmentDetailDto dto = Mapper.Map<InventoryAdjustmentDetailDto>(adjustment);
        return Result<InventoryAdjustmentDetailDto>.Success(dto);
    }

    /// <summary>
    /// Applies each adjustment line to the corresponding stock level within a transaction.
    /// Returns a failure result if any stock level would go negative.
    /// </summary>
    private async Task<Result?> ApplyAdjustmentLinesToStockAsync(
        InventoryAdjustment adjustment,
        int userId,
        CancellationToken cancellationToken)
    {
        foreach (InventoryAdjustmentLine line in adjustment.Lines)
        {
            decimal variance = line.ActualQuantity - line.ExpectedQuantity;
            if (variance == 0) continue;

            StockLevel stockLevel = await FindOrCreateStockLevelAsync(
                line.ProductId, line.WarehouseId, line.LocationId, cancellationToken).ConfigureAwait(false);

            if (stockLevel.QuantityOnHand + variance < 0)
                return Result.Failure("INSUFFICIENT_STOCK", "Applying adjustment would result in negative stock.", 409);

            stockLevel.QuantityOnHand += variance;
            stockLevel.ModifiedAtUtc = DateTime.UtcNow;

            Context.StockMovements.Add(new StockMovement
            {
                ProductId = line.ProductId,
                WarehouseId = line.WarehouseId,
                LocationId = line.LocationId,
                Quantity = variance,
                ReasonCode = StockMovementReason.Adjustment,
                ReferenceType = StockMovementReferenceType.InventoryAdjustment,
                ReferenceId = adjustment.Id,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = userId
            });
        }

        return null;
    }

    /// <summary>
    /// Finds an existing stock level or creates a new one.
    /// </summary>
    private async Task<StockLevel> FindOrCreateStockLevelAsync(
        int productId,
        int warehouseId,
        int? locationId,
        CancellationToken cancellationToken)
    {
        StockLevel? stockLevel = await Context.StockLevels
            .FirstOrDefaultAsync(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId &&
                s.LocationId == locationId,
                cancellationToken)
            .ConfigureAwait(false);

        if (stockLevel is not null)
            return stockLevel;

        stockLevel = new StockLevel
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            LocationId = locationId,
            QuantityOnHand = 0,
            QuantityReserved = 0
        };
        Context.StockLevels.Add(stockLevel);
        return stockLevel;
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
    /// Loads an adjustment with lines including product and location details.
    /// </summary>
    private async Task<InventoryAdjustment?> GetAdjustmentWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.InventoryAdjustments
            .Include(a => a.Lines).ThenInclude(l => l.Product)
            .Include(a => a.Lines).ThenInclude(l => l.Location)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the search query with optional filters.
    /// </summary>
    private IQueryable<InventoryAdjustment> BuildSearchQuery(SearchAdjustmentsRequest request)
    {
        IQueryable<InventoryAdjustment> query = Context.InventoryAdjustments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(a => a.Status == request.Status);

        if (request.DateFrom.HasValue)
            query = query.Where(a => a.CreatedAtUtc >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(a => a.CreatedAtUtc <= request.DateTo.Value);

        query = query.ApplyFilter(request.Filter);

        return query;
    }
}
