using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Fulfillment.API.Services.Base;
using Warehouse.Fulfillment.DBModel;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.Infrastructure.Caching;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Services;

/// <summary>
/// Implements sales order lifecycle operations: CRUD, lines, status transitions.
/// <para>See <see cref="ISalesOrderService"/>, <see cref="INomenclatureResolver"/>.</para>
/// </summary>
public sealed class SalesOrderService : BaseFulfillmentEntityService, ISalesOrderService
{
    private readonly IFulfillmentEventService _eventService;
    private readonly INomenclatureResolver _nomenclatureResolver;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public SalesOrderService(
        FulfillmentDbContext context,
        IMapper mapper,
        IFulfillmentEventService eventService,
        INomenclatureResolver nomenclatureResolver)
        : base(context, mapper)
    {
        _eventService = eventService;
        _nomenclatureResolver = nomenclatureResolver;
    }

    /// <inheritdoc />
    public async Task<Result<SalesOrderDetailDto>> CreateAsync(CreateSalesOrderRequest request, int userId, CancellationToken cancellationToken)
    {
        // TODO: [SDD-FULF-001 §2.1.1] Validate customer exists and is active
        // via typed HttpClient to Customers.API with Polly resilience.
        // Errors: CUSTOMER_NOT_FOUND (404), CUSTOMER_INACTIVE (409)

        // TODO: [SDD-FULF-001 §2.1.1] Validate warehouse exists
        // via typed HttpClient to Inventory.API with Polly resilience.
        // Error: INVALID_WAREHOUSE (400)

        string orderNumber = await GenerateOrderNumberAsync(cancellationToken).ConfigureAwait(false);

        SalesOrder so = new()
        {
            OrderNumber = orderNumber, CustomerId = request.CustomerId, Status = nameof(SalesOrderStatus.Draft),
            WarehouseId = request.WarehouseId, RequestedShipDate = request.RequestedShipDate,
            ShippingStreetLine1 = request.ShippingStreetLine1, ShippingStreetLine2 = request.ShippingStreetLine2,
            ShippingCity = request.ShippingCity, ShippingStateProvince = request.ShippingStateProvince,
            ShippingPostalCode = request.ShippingPostalCode, ShippingCountryCode = request.ShippingCountryCode,
            CarrierId = request.CarrierId, CarrierServiceLevelId = request.CarrierServiceLevelId,
            Notes = request.Notes, TotalAmount = 0, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = userId
        };

        foreach (CreateSalesOrderLineRequest lineReq in request.Lines)
        {
            SalesOrderLine line = new() { ProductId = lineReq.ProductId, OrderedQuantity = lineReq.OrderedQuantity, UnitPrice = lineReq.UnitPrice, LineTotal = lineReq.OrderedQuantity * lineReq.UnitPrice, Notes = lineReq.Notes };
            so.Lines.Add(line);
        }

        so.TotalAmount = so.Lines.Sum(l => l.LineTotal);
        Context.SalesOrders.Add(so);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _eventService.RecordEventAsync("SalesOrderCreated", "SalesOrder", so.Id, userId, null, cancellationToken).ConfigureAwait(false);

        return await GetByIdAsync(so.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<SalesOrderDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        SalesOrder? so = await GetSOWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (so is null) return Result<SalesOrderDetailDto>.Failure("SO_NOT_FOUND", "Sales order not found.", 404);
        SalesOrderDetailDto dto = Mapper.Map<SalesOrderDetailDto>(so);
        dto = await EnrichDetailDtoAsync(dto, cancellationToken).ConfigureAwait(false);
        return Result<SalesOrderDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<SalesOrderDto>>> SearchAsync(SearchSalesOrdersRequest request, CancellationToken cancellationToken)
    {
        IQueryable<SalesOrder> query = BuildSearchQuery(request);
        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        query = ApplySorting(query, request.SortBy, request.SortDescending);
        List<SalesOrder> items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<SalesOrderDto> dtos = Mapper.Map<IReadOnlyList<SalesOrderDto>>(items);
        PaginatedResponse<SalesOrderDto> response = new() { Items = dtos, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
        return Result<PaginatedResponse<SalesOrderDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<SalesOrderDetailDto>> UpdateHeaderAsync(int id, UpdateSalesOrderRequest request, int userId, CancellationToken cancellationToken)
    {
        SalesOrder? so = await GetSOWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (so is null) return Result<SalesOrderDetailDto>.Failure("SO_NOT_FOUND", "Sales order not found.", 404);
        if (so.Status != nameof(SalesOrderStatus.Draft)) return Result<SalesOrderDetailDto>.Failure("SO_NOT_EDITABLE", "Sales order can only be edited in Draft status.", 409);

        so.WarehouseId = request.WarehouseId; so.RequestedShipDate = request.RequestedShipDate;
        so.ShippingStreetLine1 = request.ShippingStreetLine1; so.ShippingStreetLine2 = request.ShippingStreetLine2;
        so.ShippingCity = request.ShippingCity; so.ShippingStateProvince = request.ShippingStateProvince;
        so.ShippingPostalCode = request.ShippingPostalCode; so.ShippingCountryCode = request.ShippingCountryCode;
        so.CarrierId = request.CarrierId; so.CarrierServiceLevelId = request.CarrierServiceLevelId;
        so.Notes = request.Notes; so.ModifiedAtUtc = DateTime.UtcNow; so.ModifiedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        SalesOrderDetailDto dto = Mapper.Map<SalesOrderDetailDto>(so);
        dto = await EnrichDetailDtoAsync(dto, cancellationToken).ConfigureAwait(false);
        return Result<SalesOrderDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<SalesOrderLineDto>> AddLineAsync(int soId, CreateSalesOrderLineRequest request, CancellationToken cancellationToken)
    {
        SalesOrder? so = await Context.SalesOrders.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == soId, cancellationToken).ConfigureAwait(false);
        if (so is null) return Result<SalesOrderLineDto>.Failure("SO_NOT_FOUND", "Sales order not found.", 404);
        if (so.Status != nameof(SalesOrderStatus.Draft)) return Result<SalesOrderLineDto>.Failure("SO_NOT_EDITABLE", "Sales order can only be edited in Draft status.", 409);

        bool duplicateProduct = so.Lines.Any(l => l.ProductId == request.ProductId);
        if (duplicateProduct) return Result<SalesOrderLineDto>.Failure("DUPLICATE_SO_LINE", "This product is already on the sales order.", 409);

        SalesOrderLine line = new() { SalesOrderId = soId, ProductId = request.ProductId, OrderedQuantity = request.OrderedQuantity, UnitPrice = request.UnitPrice, LineTotal = request.OrderedQuantity * request.UnitPrice, Notes = request.Notes };
        Context.SalesOrderLines.Add(line);
        so.TotalAmount = so.Lines.Sum(l => l.LineTotal) + line.LineTotal;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToResult<SalesOrderLine, SalesOrderLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<Result<SalesOrderLineDto>> UpdateLineAsync(int soId, int lineId, UpdateSalesOrderLineRequest request, CancellationToken cancellationToken)
    {
        SalesOrder? so = await Context.SalesOrders.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == soId, cancellationToken).ConfigureAwait(false);
        if (so is null) return Result<SalesOrderLineDto>.Failure("SO_NOT_FOUND", "Sales order not found.", 404);
        if (so.Status != nameof(SalesOrderStatus.Draft)) return Result<SalesOrderLineDto>.Failure("SO_NOT_EDITABLE", "Sales order can only be edited in Draft status.", 409);

        SalesOrderLine? line = so.Lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null) return Result<SalesOrderLineDto>.Failure("SO_LINE_NOT_FOUND", "Sales order line not found.", 404);

        line.OrderedQuantity = request.OrderedQuantity; line.UnitPrice = request.UnitPrice; line.LineTotal = request.OrderedQuantity * request.UnitPrice; line.Notes = request.Notes;
        so.TotalAmount = so.Lines.Sum(l => l.LineTotal);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToResult<SalesOrderLine, SalesOrderLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<Result> RemoveLineAsync(int soId, int lineId, CancellationToken cancellationToken)
    {
        SalesOrder? so = await Context.SalesOrders.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == soId, cancellationToken).ConfigureAwait(false);
        if (so is null) return Result.Failure("SO_NOT_FOUND", "Sales order not found.", 404);
        if (so.Status != nameof(SalesOrderStatus.Draft)) return Result.Failure("SO_NOT_EDITABLE", "Sales order can only be edited in Draft status.", 409);

        SalesOrderLine? line = so.Lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null) return Result.Failure("SO_LINE_NOT_FOUND", "Sales order line not found.", 404);
        if (so.Lines.Count <= 1) return Result.Failure("SO_MUST_HAVE_LINES", "Sales order must have at least one line.", 409);

        Context.SalesOrderLines.Remove(line);
        so.TotalAmount = so.Lines.Where(l => l.Id != lineId).Sum(l => l.LineTotal);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<SalesOrderDetailDto>> ConfirmAsync(int id, int userId, CancellationToken cancellationToken)
    {
        SalesOrder? so = await GetSOWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (so is null) return Result<SalesOrderDetailDto>.Failure("SO_NOT_FOUND", "Sales order not found.", 404);
        if (so.Status != nameof(SalesOrderStatus.Draft)) return Result<SalesOrderDetailDto>.Failure("INVALID_SO_STATUS_TRANSITION", $"Cannot transition sales order from {so.Status} to Confirmed.", 409);
        if (!so.Lines.Any()) return Result<SalesOrderDetailDto>.Failure("SO_MUST_HAVE_LINES", "Sales order must have at least one line.", 409);

        so.Status = nameof(SalesOrderStatus.Confirmed); so.ConfirmedAtUtc = DateTime.UtcNow; so.ConfirmedByUserId = userId; so.ModifiedAtUtc = DateTime.UtcNow; so.ModifiedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("SalesOrderConfirmed", "SalesOrder", id, userId, null, cancellationToken).ConfigureAwait(false);

        SalesOrderDetailDto dto = Mapper.Map<SalesOrderDetailDto>(so);
        dto = await EnrichDetailDtoAsync(dto, cancellationToken).ConfigureAwait(false);
        return Result<SalesOrderDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<SalesOrderDetailDto>> CancelAsync(int id, int userId, CancellationToken cancellationToken)
    {
        SalesOrder? so = await GetSOWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (so is null) return Result<SalesOrderDetailDto>.Failure("SO_NOT_FOUND", "Sales order not found.", 404);

        if (so.Status != nameof(SalesOrderStatus.Draft) && so.Status != nameof(SalesOrderStatus.Confirmed))
            return Result<SalesOrderDetailDto>.Failure("INVALID_SO_STATUS_TRANSITION", $"Cannot transition sales order from {so.Status} to Cancelled.", 409);

        bool hasPickLists = await Context.PickLists.AnyAsync(pl => pl.SalesOrderId == id, cancellationToken).ConfigureAwait(false);
        if (hasPickLists) return Result<SalesOrderDetailDto>.Failure("SO_HAS_PICK_LISTS", "Cannot cancel sales order -- pick lists have been generated.", 409);

        so.Status = nameof(SalesOrderStatus.Cancelled); so.ModifiedAtUtc = DateTime.UtcNow; so.ModifiedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("SalesOrderCancelled", "SalesOrder", id, userId, null, cancellationToken).ConfigureAwait(false);

        SalesOrderDetailDto dto = Mapper.Map<SalesOrderDetailDto>(so);
        dto = await EnrichDetailDtoAsync(dto, cancellationToken).ConfigureAwait(false);
        return Result<SalesOrderDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<SalesOrderDetailDto>> CompleteAsync(int id, int userId, CancellationToken cancellationToken)
    {
        SalesOrder? so = await GetSOWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (so is null) return Result<SalesOrderDetailDto>.Failure("SO_NOT_FOUND", "Sales order not found.", 404);
        if (so.Status == nameof(SalesOrderStatus.Completed)) return Result<SalesOrderDetailDto>.Failure("SO_ALREADY_COMPLETED", "Sales order is already completed.", 409);
        if (so.Status != nameof(SalesOrderStatus.Shipped)) return Result<SalesOrderDetailDto>.Failure("INVALID_SO_STATUS_TRANSITION", $"Cannot transition sales order from {so.Status} to Completed.", 409);

        so.Status = nameof(SalesOrderStatus.Completed); so.CompletedAtUtc = DateTime.UtcNow; so.CompletedByUserId = userId; so.ModifiedAtUtc = DateTime.UtcNow; so.ModifiedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("SalesOrderCompleted", "SalesOrder", id, userId, null, cancellationToken).ConfigureAwait(false);

        SalesOrderDetailDto dto = Mapper.Map<SalesOrderDetailDto>(so);
        dto = await EnrichDetailDtoAsync(dto, cancellationToken).ConfigureAwait(false);
        return Result<SalesOrderDetailDto>.Success(dto);
    }

    /// <summary>
    /// Resolves the shipping country name from Nomenclature cache and enriches the DTO.
    /// </summary>
    private async Task<SalesOrderDetailDto> EnrichDetailDtoAsync(
        SalesOrderDetailDto dto, CancellationToken cancellationToken)
    {
        string? countryName = await _nomenclatureResolver
            .ResolveCountryNameAsync(dto.ShippingCountryCode, cancellationToken)
            .ConfigureAwait(false);

        return dto with { ShippingCountryName = countryName };
    }

    private async Task<SalesOrder?> GetSOWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.SalesOrders.Include(so => so.Lines).FirstOrDefaultAsync(so => so.Id == id, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        string datePrefix = $"SO-{DateTime.UtcNow:yyyyMMdd}-";
        int count = await Context.SalesOrders.CountAsync(so => so.OrderNumber.StartsWith(datePrefix), cancellationToken).ConfigureAwait(false);
        return $"{datePrefix}{(count + 1):D4}";
    }

    private IQueryable<SalesOrder> BuildSearchQuery(SearchSalesOrdersRequest request)
    {
        IQueryable<SalesOrder> query = Context.SalesOrders.AsNoTracking();
        if (request.CustomerId.HasValue) query = query.Where(so => so.CustomerId == request.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(so => so.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.OrderNumber)) query = query.Where(so => so.OrderNumber.StartsWith(request.OrderNumber));
        if (request.WarehouseId.HasValue) query = query.Where(so => so.WarehouseId == request.WarehouseId.Value);
        if (request.DateFrom.HasValue) query = query.Where(so => so.CreatedAtUtc >= request.DateFrom.Value);
        if (request.DateTo.HasValue) query = query.Where(so => so.CreatedAtUtc <= request.DateTo.Value);
        return query;
    }

    private static IQueryable<SalesOrder> ApplySorting(IQueryable<SalesOrder> query, string? sortBy, bool sortDescending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "ordernumber" => sortDescending ? query.OrderByDescending(so => so.OrderNumber) : query.OrderBy(so => so.OrderNumber),
            "requestedshipdate" => sortDescending ? query.OrderByDescending(so => so.RequestedShipDate) : query.OrderBy(so => so.RequestedShipDate),
            _ => sortDescending ? query.OrderByDescending(so => so.CreatedAtUtc) : query.OrderBy(so => so.CreatedAtUtc)
        };
    }
}
