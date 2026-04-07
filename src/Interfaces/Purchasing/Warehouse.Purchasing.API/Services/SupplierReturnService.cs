using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NLog;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.Purchasing.API.Services.Base;
using Warehouse.Purchasing.DBModel;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Services;

/// <summary>
/// Implements supplier return operations: create, confirm, cancel, search.
/// <para>See <see cref="ISupplierReturnService"/>.</para>
/// </summary>
public sealed class SupplierReturnService : BasePurchasingEntityService, ISupplierReturnService
{
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IPurchaseEventService _eventService;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public SupplierReturnService(PurchasingDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint, IPurchaseEventService eventService)
        : base(context, mapper)
    {
        _publishEndpoint = publishEndpoint;
        _eventService = eventService;
    }

    /// <inheritdoc />
    public async Task<Result<SupplierReturnDetailDto>> CreateAsync(CreateSupplierReturnRequest request, int userId, CancellationToken cancellationToken)
    {
        Supplier? supplier = await Context.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId && !s.IsDeleted, cancellationToken).ConfigureAwait(false);
        if (supplier is null) return Result<SupplierReturnDetailDto>.Failure("SUPPLIER_NOT_FOUND", "Supplier not found.", 404);
        if (!supplier.IsActive) return Result<SupplierReturnDetailDto>.Failure("SUPPLIER_INACTIVE", "The supplier is inactive.", 409);

        string returnNumber = await GenerateReturnNumberAsync(cancellationToken).ConfigureAwait(false);

        SupplierReturn sr = new()
        {
            ReturnNumber = returnNumber, SupplierId = request.SupplierId, Status = nameof(SupplierReturnStatus.Draft),
            Reason = request.Reason, Notes = request.Notes, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = userId
        };

        foreach (CreateSupplierReturnLineRequest lineReq in request.Lines)
        {
            SupplierReturnLine line = new()
            {
                ProductId = lineReq.ProductId, WarehouseId = lineReq.WarehouseId, LocationId = lineReq.LocationId,
                Quantity = lineReq.Quantity, BatchId = lineReq.BatchId, GoodsReceiptLineId = lineReq.GoodsReceiptLineId,
                Notes = lineReq.Notes
            };
            sr.Lines.Add(line);
        }

        Context.SupplierReturns.Add(sr);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _eventService.RecordEventAsync("SupplierReturnCreated", "SupplierReturn", sr.Id, userId, null, cancellationToken).ConfigureAwait(false);
        return await GetByIdAsync(sr.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<SupplierReturnDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        SupplierReturn? sr = await GetReturnWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (sr is null) return Result<SupplierReturnDetailDto>.Failure("RETURN_NOT_FOUND", "Supplier return not found.", 404);
        SupplierReturnDetailDto dto = Mapper.Map<SupplierReturnDetailDto>(sr);
        return Result<SupplierReturnDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<SupplierReturnDto>>> SearchAsync(SearchSupplierReturnsRequest request, CancellationToken cancellationToken)
    {
        IQueryable<SupplierReturn> query = BuildSearchQuery(request);
        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        IQueryable<SupplierReturn> sorted = request.SortDescending
            ? query.OrderByDescending(sr => sr.CreatedAtUtc)
            : query.OrderBy(sr => sr.CreatedAtUtc);

        List<SupplierReturn> items = await sorted.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<SupplierReturnDto> dtos = Mapper.Map<IReadOnlyList<SupplierReturnDto>>(items);
        PaginatedResponse<SupplierReturnDto> response = new() { Items = dtos, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
        return Result<PaginatedResponse<SupplierReturnDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<SupplierReturnDetailDto>> ConfirmAsync(int id, int userId, CancellationToken cancellationToken)
    {
        SupplierReturn? sr = await GetReturnWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (sr is null) return Result<SupplierReturnDetailDto>.Failure("RETURN_NOT_FOUND", "Supplier return not found.", 404);
        if (sr.Status == nameof(SupplierReturnStatus.Confirmed)) return Result<SupplierReturnDetailDto>.Failure("RETURN_ALREADY_CONFIRMED", "Supplier return has already been confirmed.", 409);
        if (sr.Status != nameof(SupplierReturnStatus.Draft)) return Result<SupplierReturnDetailDto>.Failure("INVALID_RETURN_STATUS", "Only draft supplier returns can be confirmed.", 409);

        // TODO: [SDD-PURCH-001 §2.7.2] Validate sufficient stock for each return line
        // by calling Inventory.API via typed HttpClient with Polly resilience.
        // This is the first Polly use case — implement when AddWarehouseHttpClient is consumed.
        // Error: INSUFFICIENT_STOCK (409) if any line exceeds available stock.

        sr.Status = nameof(SupplierReturnStatus.Confirmed); sr.ConfirmedAtUtc = DateTime.UtcNow; sr.ConfirmedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _publishEndpoint.Publish(new SupplierReturnCompletedEvent
            {
                SupplierReturnId = sr.Id, SupplierReturnNumber = sr.ReturnNumber, SupplierId = sr.SupplierId,
                ConfirmedByUserId = userId, ConfirmedAtUtc = sr.ConfirmedAtUtc!.Value,
                Lines = sr.Lines.Select(l => new SupplierReturnCompletedLine
                {
                    SupplierReturnLineId = l.Id, ProductId = l.ProductId, WarehouseId = l.WarehouseId,
                    LocationId = l.LocationId, Quantity = l.Quantity, BatchId = l.BatchId
                }).ToList()
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) { Logger.Warn(ex, "Failed to publish {EventType} event", "SupplierReturnCompleted"); }

        await _eventService.RecordEventAsync("SupplierReturnConfirmed", "SupplierReturn", id, userId, null, cancellationToken).ConfigureAwait(false);

        SupplierReturnDetailDto dto = Mapper.Map<SupplierReturnDetailDto>(sr);
        return Result<SupplierReturnDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<SupplierReturnDetailDto>> CancelAsync(int id, int userId, CancellationToken cancellationToken)
    {
        SupplierReturn? sr = await GetReturnWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (sr is null) return Result<SupplierReturnDetailDto>.Failure("RETURN_NOT_FOUND", "Supplier return not found.", 404);
        if (sr.Status == nameof(SupplierReturnStatus.Confirmed)) return Result<SupplierReturnDetailDto>.Failure("RETURN_ALREADY_CONFIRMED", "Supplier return has already been confirmed.", 409);
        if (sr.Status != nameof(SupplierReturnStatus.Draft)) return Result<SupplierReturnDetailDto>.Failure("INVALID_RETURN_STATUS", "Only draft supplier returns can be cancelled.", 409);

        sr.Status = nameof(SupplierReturnStatus.Cancelled);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("SupplierReturnCancelled", "SupplierReturn", id, userId, null, cancellationToken).ConfigureAwait(false);

        SupplierReturnDetailDto dto = Mapper.Map<SupplierReturnDetailDto>(sr);
        return Result<SupplierReturnDetailDto>.Success(dto);
    }

    private async Task<SupplierReturn?> GetReturnWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.SupplierReturns.Include(sr => sr.Supplier).Include(sr => sr.Lines).FirstOrDefaultAsync(sr => sr.Id == id, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GenerateReturnNumberAsync(CancellationToken cancellationToken)
    {
        string datePrefix = $"SR-{DateTime.UtcNow:yyyyMMdd}-";
        int count = await Context.SupplierReturns.CountAsync(sr => sr.ReturnNumber.StartsWith(datePrefix), cancellationToken).ConfigureAwait(false);
        return $"{datePrefix}{(count + 1):D4}";
    }

    private IQueryable<SupplierReturn> BuildSearchQuery(SearchSupplierReturnsRequest request)
    {
        IQueryable<SupplierReturn> query = Context.SupplierReturns.AsNoTracking().Include(sr => sr.Supplier);
        if (request.SupplierId.HasValue) query = query.Where(sr => sr.SupplierId == request.SupplierId.Value);
        if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(sr => sr.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.ReturnNumber)) query = query.Where(sr => sr.ReturnNumber.StartsWith(request.ReturnNumber));
        if (request.DateFrom.HasValue) query = query.Where(sr => sr.CreatedAtUtc >= request.DateFrom.Value);
        if (request.DateTo.HasValue) query = query.Where(sr => sr.CreatedAtUtc <= request.DateTo.Value);
        return query;
    }
}
