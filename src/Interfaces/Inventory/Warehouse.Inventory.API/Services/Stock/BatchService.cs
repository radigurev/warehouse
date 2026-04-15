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
/// Implements batch lifecycle operations: CRUD and search.
/// <para>See <see cref="IBatchService"/>.</para>
/// </summary>
public sealed class BatchService : BaseInventoryEntityService, IBatchService
{
    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public BatchService(InventoryDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }

    /// <inheritdoc />
    public async Task<Result<BatchDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Batch? batch = await Context.Batches
            .AsNoTracking()
            .Include(b => b.Product)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
            return Result<BatchDto>.Failure("BATCH_NOT_FOUND", "Batch not found.", 404);

        BatchDto dto = Mapper.Map<BatchDto>(batch);
        await ResolveQuantitiesAsync([dto], cancellationToken).ConfigureAwait(false);
        return Result<BatchDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<BatchDto>>> SearchAsync(
        SearchBatchesRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<Batch> query = BuildSearchQuery(request);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<Batch> items = await query
            .OrderBy(b => b.BatchNumber)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<BatchDto> dtos = Mapper.Map<IReadOnlyList<BatchDto>>(items);
        await ResolveQuantitiesAsync(dtos, cancellationToken).ConfigureAwait(false);

        PaginatedResponse<BatchDto> response = new()
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<PaginatedResponse<BatchDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<BatchDto>> CreateAsync(
        CreateBatchRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        Result? productValidation = await ValidateProductExistsAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (productValidation is not null)
            return Result<BatchDto>.Failure(productValidation.ErrorCode!, productValidation.ErrorMessage!, productValidation.StatusCode!.Value);

        Result? uniqueValidation = await ValidateUniqueBatchNumberAsync(request.ProductId, request.BatchNumber, null, cancellationToken).ConfigureAwait(false);
        if (uniqueValidation is not null)
            return Result<BatchDto>.Failure(uniqueValidation.ErrorCode!, uniqueValidation.ErrorMessage!, uniqueValidation.StatusCode!.Value);

        Batch batch = new()
        {
            ProductId = request.ProductId,
            BatchNumber = request.BatchNumber,
            ExpiryDate = request.ExpiryDate,
            Notes = request.Notes,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        Context.Batches.Add(batch);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Batch? created = await Context.Batches
            .Include(b => b.Product)
            .FirstOrDefaultAsync(b => b.Id == batch.Id, cancellationToken)
            .ConfigureAwait(false);

        BatchDto dto = Mapper.Map<BatchDto>(created!);
        return Result<BatchDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<BatchDto>> UpdateAsync(
        int id,
        UpdateBatchRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        Batch? batch = await Context.Batches
            .Include(b => b.Product)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
            return Result<BatchDto>.Failure("BATCH_NOT_FOUND", "Batch not found.", 404);

        batch.ExpiryDate = request.ExpiryDate;
        batch.Notes = request.Notes;
        batch.ModifiedAtUtc = DateTime.UtcNow;
        batch.ModifiedByUserId = userId;

        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        BatchDto dto = Mapper.Map<BatchDto>(batch);
        return Result<BatchDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        Batch? batch = await Context.Batches
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
            return Result.Failure("BATCH_NOT_FOUND", "Batch not found.", 404);

        batch.IsActive = false;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// Resolves total quantity on hand for each batch from stock levels.
    /// </summary>
    private async Task ResolveQuantitiesAsync(IReadOnlyList<BatchDto> dtos, CancellationToken cancellationToken)
    {
        List<int> batchIds = dtos.Select(d => d.Id).ToList();
        if (batchIds.Count == 0) return;

        Dictionary<int, decimal> quantityMap = await Context.StockLevels
            .Where(sl => sl.BatchId.HasValue && batchIds.Contains(sl.BatchId.Value))
            .GroupBy(sl => sl.BatchId!.Value)
            .Select(g => new { BatchId = g.Key, Total = g.Sum(sl => sl.QuantityOnHand) })
            .ToDictionaryAsync(x => x.BatchId, x => x.Total, cancellationToken)
            .ConfigureAwait(false);

        foreach (BatchDto dto in dtos)
        {
            dto.QuantityOnHand = quantityMap.GetValueOrDefault(dto.Id, 0m);
        }
    }

    /// <summary>
    /// Builds the search query with optional filters.
    /// </summary>
    private IQueryable<Batch> BuildSearchQuery(SearchBatchesRequest request)
    {
        IQueryable<Batch> query = Context.Batches
            .AsNoTracking()
            .Include(b => b.Product);

        if (request.ProductId.HasValue)
            query = query.Where(b => b.ProductId == request.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(request.BatchNumber))
            query = query.Where(b => b.BatchNumber.Contains(request.BatchNumber));

        if (!request.IncludeExpired)
            query = query.Where(b => b.ExpiryDate == null || b.ExpiryDate >= DateOnly.FromDateTime(DateTime.UtcNow));

        query = query.ApplyFilter(request.Filter);

        return query;
    }

    /// <summary>
    /// Validates that a product exists and is not deleted.
    /// </summary>
    private async Task<Result?> ValidateProductExistsAsync(int productId, CancellationToken cancellationToken)
    {
        bool exists = await Context.Products
            .AnyAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return exists ? null : Result.Failure("INVALID_PRODUCT", "The specified product does not exist.", 400);
    }

    /// <summary>
    /// Validates batch number uniqueness per product.
    /// </summary>
    private async Task<Result?> ValidateUniqueBatchNumberAsync(
        int productId,
        string batchNumber,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        IQueryable<Batch> query = Context.Batches
            .Where(b => b.ProductId == productId && b.BatchNumber == batchNumber);

        if (excludeId.HasValue)
            query = query.Where(b => b.Id != excludeId.Value);

        bool exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

        return exists
            ? Result.Failure("DUPLICATE_BATCH_NUMBER", "A batch with this number already exists for the product.", 409)
            : null;
    }
}
