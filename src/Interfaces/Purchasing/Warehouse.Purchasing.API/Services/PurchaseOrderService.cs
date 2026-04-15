using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.Purchasing.API.Services.Base;
using Warehouse.Purchasing.DBModel;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Services;

/// <summary>
/// Implements purchase order lifecycle operations: CRUD, lines, status transitions.
/// <para>See <see cref="IPurchaseOrderService"/>.</para>
/// </summary>
public sealed class PurchaseOrderService : BasePurchasingEntityService, IPurchaseOrderService
{
    private readonly IPurchaseEventService _eventService;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public PurchaseOrderService(PurchasingDbContext context, IMapper mapper, IPurchaseEventService eventService)
        : base(context, mapper)
    {
        _eventService = eventService;
    }

    /// <inheritdoc />
    public async Task<Result<PurchaseOrderDetailDto>> CreateAsync(CreatePurchaseOrderRequest request, int userId, CancellationToken cancellationToken)
    {
        Result? supplierValidation = await ValidateSupplierForPOAsync(request.SupplierId, cancellationToken).ConfigureAwait(false);
        if (supplierValidation is not null) return Result<PurchaseOrderDetailDto>.Failure(supplierValidation.ErrorCode!, supplierValidation.ErrorMessage!, supplierValidation.StatusCode!.Value);

        string orderNumber = await GenerateOrderNumberAsync(cancellationToken).ConfigureAwait(false);

        PurchaseOrder po = new()
        {
            OrderNumber = orderNumber, SupplierId = request.SupplierId, Status = nameof(PurchaseOrderStatus.Draft),
            DestinationWarehouseId = request.DestinationWarehouseId, ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Notes = request.Notes, TotalAmount = 0, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = userId
        };

        foreach (CreatePurchaseOrderLineRequest lineReq in request.Lines)
        {
            PurchaseOrderLine line = new() { ProductId = lineReq.ProductId, OrderedQuantity = lineReq.OrderedQuantity, UnitPrice = lineReq.UnitPrice, LineTotal = lineReq.OrderedQuantity * lineReq.UnitPrice, ReceivedQuantity = 0, Notes = lineReq.Notes };
            po.Lines.Add(line);
        }

        po.TotalAmount = po.Lines.Sum(l => l.LineTotal);
        Context.PurchaseOrders.Add(po);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _eventService.RecordEventAsync("PurchaseOrderCreated", "PurchaseOrder", po.Id, userId, null, cancellationToken).ConfigureAwait(false);

        return await GetByIdAsync(po.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<PurchaseOrderDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        PurchaseOrder? po = await GetPOWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (po is null) return Result<PurchaseOrderDetailDto>.Failure("PO_NOT_FOUND", "Purchase order not found.", 404);
        PurchaseOrderDetailDto dto = await MapToDetailDtoAsync(po, cancellationToken).ConfigureAwait(false);
        return Result<PurchaseOrderDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<PurchaseOrderDto>>> SearchAsync(SearchPurchaseOrdersRequest request, CancellationToken cancellationToken)
    {
        IQueryable<PurchaseOrder> query = BuildSearchQuery(request);
        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        query = ApplySorting(query, request.SortBy, request.SortDescending);
        List<PurchaseOrder> items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<PurchaseOrderDto> dtos = Mapper.Map<IReadOnlyList<PurchaseOrderDto>>(items);
        PaginatedResponse<PurchaseOrderDto> response = new() { Items = dtos, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
        return Result<PaginatedResponse<PurchaseOrderDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<PurchaseOrderDetailDto>> UpdateHeaderAsync(int id, UpdatePurchaseOrderRequest request, int userId, CancellationToken cancellationToken)
    {
        PurchaseOrder? po = await GetPOWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (po is null) return Result<PurchaseOrderDetailDto>.Failure("PO_NOT_FOUND", "Purchase order not found.", 404);
        if (po.Status != nameof(PurchaseOrderStatus.Draft)) return Result<PurchaseOrderDetailDto>.Failure("PO_NOT_EDITABLE", "Purchase order can only be edited in Draft status.", 409);

        po.DestinationWarehouseId = request.DestinationWarehouseId; po.ExpectedDeliveryDate = request.ExpectedDeliveryDate; po.Notes = request.Notes;
        po.ModifiedAtUtc = DateTime.UtcNow; po.ModifiedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        PurchaseOrderDetailDto dto = await MapToDetailDtoAsync(po, cancellationToken).ConfigureAwait(false);
        return Result<PurchaseOrderDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PurchaseOrderLineDto>> AddLineAsync(int poId, CreatePurchaseOrderLineRequest request, CancellationToken cancellationToken)
    {
        PurchaseOrder? po = await Context.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == poId, cancellationToken).ConfigureAwait(false);
        if (po is null) return Result<PurchaseOrderLineDto>.Failure("PO_NOT_FOUND", "Purchase order not found.", 404);
        if (po.Status != nameof(PurchaseOrderStatus.Draft)) return Result<PurchaseOrderLineDto>.Failure("PO_NOT_EDITABLE", "Purchase order can only be edited in Draft status.", 409);

        bool duplicateProduct = po.Lines.Any(l => l.ProductId == request.ProductId);
        if (duplicateProduct) return Result<PurchaseOrderLineDto>.Failure("DUPLICATE_PO_LINE", "This product is already on the purchase order.", 409);

        PurchaseOrderLine line = new() { PurchaseOrderId = poId, ProductId = request.ProductId, OrderedQuantity = request.OrderedQuantity, UnitPrice = request.UnitPrice, LineTotal = request.OrderedQuantity * request.UnitPrice, ReceivedQuantity = 0, Notes = request.Notes };
        Context.PurchaseOrderLines.Add(line);
        po.TotalAmount = po.Lines.Sum(l => l.LineTotal) + line.LineTotal;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToResult<PurchaseOrderLine, PurchaseOrderLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<Result<PurchaseOrderLineDto>> UpdateLineAsync(int poId, int lineId, UpdatePurchaseOrderLineRequest request, CancellationToken cancellationToken)
    {
        PurchaseOrder? po = await Context.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == poId, cancellationToken).ConfigureAwait(false);
        if (po is null) return Result<PurchaseOrderLineDto>.Failure("PO_NOT_FOUND", "Purchase order not found.", 404);
        if (po.Status != nameof(PurchaseOrderStatus.Draft)) return Result<PurchaseOrderLineDto>.Failure("PO_NOT_EDITABLE", "Purchase order can only be edited in Draft status.", 409);

        PurchaseOrderLine? line = po.Lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null) return Result<PurchaseOrderLineDto>.Failure("PO_LINE_NOT_FOUND", "Purchase order line not found.", 404);

        line.OrderedQuantity = request.OrderedQuantity; line.UnitPrice = request.UnitPrice; line.LineTotal = request.OrderedQuantity * request.UnitPrice; line.Notes = request.Notes;
        po.TotalAmount = po.Lines.Sum(l => l.LineTotal);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToResult<PurchaseOrderLine, PurchaseOrderLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<Result> RemoveLineAsync(int poId, int lineId, CancellationToken cancellationToken)
    {
        PurchaseOrder? po = await Context.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == poId, cancellationToken).ConfigureAwait(false);
        if (po is null) return Result.Failure("PO_NOT_FOUND", "Purchase order not found.", 404);
        if (po.Status != nameof(PurchaseOrderStatus.Draft)) return Result.Failure("PO_NOT_EDITABLE", "Purchase order can only be edited in Draft status.", 409);

        PurchaseOrderLine? line = po.Lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null) return Result.Failure("PO_LINE_NOT_FOUND", "Purchase order line not found.", 404);
        if (po.Lines.Count <= 1) return Result.Failure("PO_MUST_HAVE_LINES", "Purchase order must have at least one line.", 409);

        Context.PurchaseOrderLines.Remove(line);
        po.TotalAmount = po.Lines.Where(l => l.Id != lineId).Sum(l => l.LineTotal);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<PurchaseOrderDetailDto>> ConfirmAsync(int id, int userId, CancellationToken cancellationToken)
    {
        PurchaseOrder? po = await GetPOWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (po is null) return Result<PurchaseOrderDetailDto>.Failure("PO_NOT_FOUND", "Purchase order not found.", 404);
        if (po.Status != nameof(PurchaseOrderStatus.Draft)) return Result<PurchaseOrderDetailDto>.Failure("INVALID_PO_STATUS_TRANSITION", $"Cannot transition purchase order from {po.Status} to Confirmed.", 409);
        if (!po.Lines.Any()) return Result<PurchaseOrderDetailDto>.Failure("PO_MUST_HAVE_LINES", "Purchase order must have at least one line.", 409);

        po.Status = nameof(PurchaseOrderStatus.Confirmed); po.ConfirmedAtUtc = DateTime.UtcNow; po.ConfirmedByUserId = userId; po.ModifiedAtUtc = DateTime.UtcNow; po.ModifiedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("PurchaseOrderConfirmed", "PurchaseOrder", id, userId, null, cancellationToken).ConfigureAwait(false);

        PurchaseOrderDetailDto dto = await MapToDetailDtoAsync(po, cancellationToken).ConfigureAwait(false);
        return Result<PurchaseOrderDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PurchaseOrderDetailDto>> CancelAsync(int id, int userId, CancellationToken cancellationToken)
    {
        PurchaseOrder? po = await GetPOWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (po is null) return Result<PurchaseOrderDetailDto>.Failure("PO_NOT_FOUND", "Purchase order not found.", 404);

        if (po.Status != nameof(PurchaseOrderStatus.Draft) && po.Status != nameof(PurchaseOrderStatus.Confirmed))
            return Result<PurchaseOrderDetailDto>.Failure("INVALID_PO_STATUS_TRANSITION", $"Cannot transition purchase order from {po.Status} to Cancelled.", 409);

        bool hasReceipts = await Context.GoodsReceipts.AnyAsync(gr => gr.PurchaseOrderId == id, cancellationToken).ConfigureAwait(false);
        if (hasReceipts) return Result<PurchaseOrderDetailDto>.Failure("PO_HAS_RECEIPTS", "Cannot cancel purchase order -- goods have been received.", 409);

        po.Status = nameof(PurchaseOrderStatus.Cancelled); po.ModifiedAtUtc = DateTime.UtcNow; po.ModifiedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("PurchaseOrderCancelled", "PurchaseOrder", id, userId, null, cancellationToken).ConfigureAwait(false);

        PurchaseOrderDetailDto dto = await MapToDetailDtoAsync(po, cancellationToken).ConfigureAwait(false);
        return Result<PurchaseOrderDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PurchaseOrderDetailDto>> CloseAsync(int id, int userId, CancellationToken cancellationToken)
    {
        PurchaseOrder? po = await GetPOWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (po is null) return Result<PurchaseOrderDetailDto>.Failure("PO_NOT_FOUND", "Purchase order not found.", 404);
        if (po.Status == nameof(PurchaseOrderStatus.Closed)) return Result<PurchaseOrderDetailDto>.Failure("PO_ALREADY_CLOSED", "Purchase order is already closed.", 409);

        if (po.Status != nameof(PurchaseOrderStatus.PartiallyReceived) && po.Status != nameof(PurchaseOrderStatus.Received))
            return Result<PurchaseOrderDetailDto>.Failure("INVALID_PO_STATUS_TRANSITION", $"Cannot transition purchase order from {po.Status} to Closed.", 409);

        po.Status = nameof(PurchaseOrderStatus.Closed); po.ClosedAtUtc = DateTime.UtcNow; po.ClosedByUserId = userId; po.ModifiedAtUtc = DateTime.UtcNow; po.ModifiedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("PurchaseOrderClosed", "PurchaseOrder", id, userId, null, cancellationToken).ConfigureAwait(false);

        PurchaseOrderDetailDto dto = await MapToDetailDtoAsync(po, cancellationToken).ConfigureAwait(false);
        return Result<PurchaseOrderDetailDto>.Success(dto);
    }

    private async Task<PurchaseOrder?> GetPOWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.PurchaseOrders.Include(po => po.Supplier).Include(po => po.Lines).FirstOrDefaultAsync(po => po.Id == id, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PurchaseOrderDetailDto> MapToDetailDtoAsync(PurchaseOrder po, CancellationToken cancellationToken)
    {
        PurchaseOrderDetailDto dto = Mapper.Map<PurchaseOrderDetailDto>(po);
        await ResolveProductDataAsync(dto.Lines, cancellationToken).ConfigureAwait(false);
        return dto;
    }

    private async Task ResolveProductDataAsync(IReadOnlyList<PurchaseOrderLineDto> lines, CancellationToken cancellationToken)
    {
        List<int> productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        if (productIds.Count == 0) return;

        Dictionary<int, ProductLookup> lookupMap = await Context.ProductLookups
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (PurchaseOrderLineDto line in lines)
        {
            if (lookupMap.TryGetValue(line.ProductId, out ProductLookup? lookup))
            {
                line.ProductName = lookup.Name;
                line.ProductCode = lookup.Code;
            }
            else
            {
                line.ProductName = $"Product #{line.ProductId}";
            }
        }
    }

    private async Task<Result?> ValidateSupplierForPOAsync(int supplierId, CancellationToken cancellationToken)
    {
        Supplier? supplier = await Context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken).ConfigureAwait(false);
        if (supplier is null || supplier.IsDeleted) return Result.Failure("SUPPLIER_NOT_FOUND", "Supplier not found.", 404);
        if (!supplier.IsActive) return Result.Failure("SUPPLIER_INACTIVE", "The supplier is inactive and cannot be used for new purchase orders.", 409);
        return null;
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        string datePrefix = $"PO-{DateTime.UtcNow:yyyyMMdd}-";
        int count = await Context.PurchaseOrders.CountAsync(po => po.OrderNumber.StartsWith(datePrefix), cancellationToken).ConfigureAwait(false);
        return $"{datePrefix}{(count + 1):D4}";
    }

    private IQueryable<PurchaseOrder> BuildSearchQuery(SearchPurchaseOrdersRequest request)
    {
        IQueryable<PurchaseOrder> query = Context.PurchaseOrders.AsNoTracking().Include(po => po.Supplier);
        if (request.SupplierId.HasValue) query = query.Where(po => po.SupplierId == request.SupplierId.Value);
        if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(po => po.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.OrderNumber)) query = query.Where(po => po.OrderNumber.StartsWith(request.OrderNumber));
        if (request.DestinationWarehouseId.HasValue) query = query.Where(po => po.DestinationWarehouseId == request.DestinationWarehouseId.Value);
        if (request.DateFrom.HasValue) query = query.Where(po => po.CreatedAtUtc >= request.DateFrom.Value);
        if (request.DateTo.HasValue) query = query.Where(po => po.CreatedAtUtc <= request.DateTo.Value);
        return query;
    }

    private static IQueryable<PurchaseOrder> ApplySorting(IQueryable<PurchaseOrder> query, string? sortBy, bool sortDescending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "ordernumber" => sortDescending ? query.OrderByDescending(po => po.OrderNumber) : query.OrderBy(po => po.OrderNumber),
            "expecteddeliverydate" => sortDescending ? query.OrderByDescending(po => po.ExpectedDeliveryDate) : query.OrderBy(po => po.ExpectedDeliveryDate),
            "suppliername" => sortDescending ? query.OrderByDescending(po => po.Supplier.Name) : query.OrderBy(po => po.Supplier.Name),
            _ => sortDescending ? query.OrderByDescending(po => po.CreatedAtUtc) : query.OrderBy(po => po.CreatedAtUtc)
        };
    }
}
