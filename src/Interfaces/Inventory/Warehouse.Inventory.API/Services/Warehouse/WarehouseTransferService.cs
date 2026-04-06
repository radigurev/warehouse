using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.GenericFiltering;
using Warehouse.Inventory.API.Interfaces.Warehouse;
using Warehouse.Inventory.API.Services.Base;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Services.Warehouse;

/// <summary>
/// Implements warehouse transfer operations: create, complete, cancel, get, and search.
/// <para>See <see cref="IWarehouseTransferService"/>.</para>
/// </summary>
public sealed class WarehouseTransferService : BaseInventoryEntityService, IWarehouseTransferService
{
    private readonly IPublishEndpoint _publishEndpoint;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public WarehouseTransferService(InventoryDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint)
        : base(context, mapper)
    {
        _publishEndpoint = publishEndpoint;
    }

    /// <inheritdoc />
    public async Task<Result<WarehouseTransferDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        WarehouseTransfer? transfer = await GetTransferWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (transfer is null)
            return Result<WarehouseTransferDetailDto>.Failure("TRANSFER_NOT_FOUND", "Warehouse transfer not found.", 404);

        WarehouseTransferDetailDto dto = Mapper.Map<WarehouseTransferDetailDto>(transfer);
        return Result<WarehouseTransferDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<WarehouseTransferDto>>> SearchAsync(
        SearchTransfersRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<WarehouseTransfer> query = BuildSearchQuery(request);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<WarehouseTransfer> items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<WarehouseTransferDto> dtos = Mapper.Map<IReadOnlyList<WarehouseTransferDto>>(items);

        PaginatedResponse<WarehouseTransferDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<WarehouseTransferDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<WarehouseTransferDetailDto>> CreateAsync(
        CreateTransferRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        if (request.SourceWarehouseId == request.DestinationWarehouseId)
            return Result<WarehouseTransferDetailDto>.Failure("TRANSFER_SAME_WAREHOUSE", "Source and destination warehouses must be different.", 400);

        WarehouseTransfer transfer = new()
        {
            SourceWarehouseId = request.SourceWarehouseId,
            DestinationWarehouseId = request.DestinationWarehouseId,
            Status = "Draft",
            Notes = request.Notes,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        foreach (CreateTransferLineRequest lineReq in request.Lines)
        {
            transfer.Lines.Add(new WarehouseTransferLine
            {
                ProductId = lineReq.ProductId,
                Quantity = lineReq.Quantity,
                SourceLocationId = lineReq.SourceLocationId,
                DestinationLocationId = lineReq.DestinationLocationId
            });
        }

        Context.WarehouseTransfers.Add(transfer);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        WarehouseTransfer? created = await GetTransferWithDetailsAsync(transfer.Id, cancellationToken).ConfigureAwait(false);
        WarehouseTransferDetailDto dto = Mapper.Map<WarehouseTransferDetailDto>(created!);
        return Result<WarehouseTransferDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<WarehouseTransferDetailDto>> CompleteAsync(
        int id,
        CompleteTransferRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        WarehouseTransfer? transfer = await GetTransferWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);

        if (transfer is null)
            return Result<WarehouseTransferDetailDto>.Failure("TRANSFER_NOT_FOUND", "Warehouse transfer not found.", 404);

        if (transfer.Status != "Draft")
            return Result<WarehouseTransferDetailDto>.Failure("TRANSFER_NOT_DRAFT", "Only draft transfers can be completed.", 409);

        await using IDbContextTransaction transaction = await Context.Database
            .BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        Result? stockValidation = await ApplyTransferLinesToStockAsync(transfer, userId, cancellationToken).ConfigureAwait(false);
        if (stockValidation is not null)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return Result<WarehouseTransferDetailDto>.Failure(stockValidation.ErrorCode!, stockValidation.ErrorMessage!, stockValidation.StatusCode!.Value);
        }

        transfer.Status = "Completed";
        transfer.CompletedAtUtc = DateTime.UtcNow;
        transfer.CompletedByUserId = userId;

        if (!string.IsNullOrWhiteSpace(request.Notes))
            transfer.Notes = $"{transfer.Notes}\n[Completion] {request.Notes}".Trim();

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _publishEndpoint.Publish(new WarehouseTransferCompletedEvent
            {
                TransferId = transfer.Id,
                SourceWarehouseId = transfer.SourceWarehouseId,
                DestinationWarehouseId = transfer.DestinationWarehouseId,
                CompletedByUserId = userId,
                CompletedAt = transfer.CompletedAtUtc!.Value
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
        }

        WarehouseTransferDetailDto dto = Mapper.Map<WarehouseTransferDetailDto>(transfer);
        return Result<WarehouseTransferDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> CancelAsync(int id, CancellationToken cancellationToken)
    {
        WarehouseTransfer? transfer = await Context.WarehouseTransfers
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (transfer is null)
            return Result.Failure("TRANSFER_NOT_FOUND", "Warehouse transfer not found.", 404);

        if (transfer.Status != "Draft")
            return Result.Failure("TRANSFER_NOT_DRAFT", "Only draft transfers can be cancelled.", 409);

        transfer.Status = "Cancelled";
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// Applies each transfer line by moving stock between warehouses.
    /// Returns a failure result if any source stock level is insufficient.
    /// </summary>
    private async Task<Result?> ApplyTransferLinesToStockAsync(
        WarehouseTransfer transfer,
        int userId,
        CancellationToken cancellationToken)
    {
        foreach (WarehouseTransferLine line in transfer.Lines)
        {
            Result? lineResult = await AdjustStockForTransferLineAsync(transfer, line, userId, cancellationToken).ConfigureAwait(false);
            if (lineResult is not null)
                return lineResult;
        }

        return null;
    }

    /// <summary>
    /// Adjusts stock levels and records movements for a single transfer line.
    /// Returns a failure result if the source stock level is insufficient.
    /// </summary>
    private async Task<Result?> AdjustStockForTransferLineAsync(
        WarehouseTransfer transfer,
        WarehouseTransferLine line,
        int userId,
        CancellationToken cancellationToken)
    {
        StockLevel sourceLevel = await FindOrCreateStockLevelAsync(
            line.ProductId, transfer.SourceWarehouseId, line.SourceLocationId, cancellationToken).ConfigureAwait(false);

        if (sourceLevel.QuantityOnHand < line.Quantity)
            return Result.Failure("INSUFFICIENT_STOCK", "Insufficient stock at source for transfer.", 409);

        sourceLevel.QuantityOnHand -= line.Quantity;
        sourceLevel.ModifiedAtUtc = DateTime.UtcNow;

        StockLevel destLevel = await FindOrCreateStockLevelAsync(
            line.ProductId, transfer.DestinationWarehouseId, line.DestinationLocationId, cancellationToken).ConfigureAwait(false);
        destLevel.QuantityOnHand += line.Quantity;
        destLevel.ModifiedAtUtc = DateTime.UtcNow;

        Context.StockMovements.Add(CreateTransferMovement(line, transfer, userId, -line.Quantity, transfer.SourceWarehouseId, line.SourceLocationId));
        Context.StockMovements.Add(CreateTransferMovement(line, transfer, userId, line.Quantity, transfer.DestinationWarehouseId, line.DestinationLocationId));

        return null;
    }

    /// <summary>
    /// Creates a stock movement for a transfer operation.
    /// </summary>
    private static StockMovement CreateTransferMovement(
        WarehouseTransferLine line,
        WarehouseTransfer transfer,
        int userId,
        decimal quantity,
        int warehouseId,
        int? locationId)
    {
        return new StockMovement
        {
            ProductId = line.ProductId,
            WarehouseId = warehouseId,
            LocationId = locationId,
            Quantity = quantity,
            ReasonCode = StockMovementReason.Transfer,
            ReferenceType = StockMovementReferenceType.WarehouseTransfer,
            ReferenceId = transfer.Id,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };
    }

    /// <summary>
    /// Finds or creates a stock level record.
    /// </summary>
    private async Task<StockLevel> FindOrCreateStockLevelAsync(
        int productId,
        int warehouseId,
        int? locationId,
        CancellationToken cancellationToken)
    {
        StockLevel? level = await Context.StockLevels
            .FirstOrDefaultAsync(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId &&
                s.LocationId == locationId,
                cancellationToken)
            .ConfigureAwait(false);

        if (level is not null)
            return level;

        level = new StockLevel
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            LocationId = locationId,
            QuantityOnHand = 0,
            QuantityReserved = 0
        };
        Context.StockLevels.Add(level);
        return level;
    }

    /// <summary>
    /// Loads a transfer with source/destination warehouses and lines.
    /// </summary>
    private async Task<WarehouseTransfer?> GetTransferWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.WarehouseTransfers
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .Include(t => t.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the search query with optional filters.
    /// </summary>
    private IQueryable<WarehouseTransfer> BuildSearchQuery(SearchTransfersRequest request)
    {
        IQueryable<WarehouseTransfer> query = Context.WarehouseTransfers
            .AsNoTracking()
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse);

        if (request.SourceWarehouseId.HasValue)
            query = query.Where(t => t.SourceWarehouseId == request.SourceWarehouseId.Value);

        if (request.DestinationWarehouseId.HasValue)
            query = query.Where(t => t.DestinationWarehouseId == request.DestinationWarehouseId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(t => t.Status == request.Status);

        if (request.DateFrom.HasValue)
            query = query.Where(t => t.CreatedAtUtc >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(t => t.CreatedAtUtc <= request.DateTo.Value);

        query = query.ApplyFilter(request.Filter);

        return query;
    }
}
