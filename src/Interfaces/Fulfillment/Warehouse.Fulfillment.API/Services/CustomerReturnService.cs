using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NLog;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Fulfillment.API.Services.Base;
using Warehouse.Fulfillment.DBModel;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.Infrastructure.Correlation;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Services;

/// <summary>
/// Implements customer return (RMA) operations: create, confirm, receive, close, cancel, search.
/// <para>See <see cref="ICustomerReturnService"/>.</para>
/// </summary>
public sealed class CustomerReturnService : BaseFulfillmentEntityService, ICustomerReturnService
{
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly IFulfillmentEventService _eventService;
    private readonly IFulfillmentLookupResolver _lookupResolver;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public CustomerReturnService(
        FulfillmentDbContext context,
        IMapper mapper,
        IPublishEndpoint publishEndpoint,
        ICorrelationIdAccessor correlationIdAccessor,
        IFulfillmentEventService eventService,
        IFulfillmentLookupResolver lookupResolver)
        : base(context, mapper)
    {
        _publishEndpoint = publishEndpoint;
        _correlationIdAccessor = correlationIdAccessor;
        _eventService = eventService;
        _lookupResolver = lookupResolver;
    }

    /// <inheritdoc />
    public async Task<Result<CustomerReturnDetailDto>> CreateAsync(CreateCustomerReturnRequest request, int userId, CancellationToken cancellationToken)
    {
        // TODO: [SDD-FULF-001 §2.7.1] Validate customer exists
        // via typed HttpClient to Customers.API with Polly resilience.
        // Error: CUSTOMER_NOT_FOUND (404)

        string returnNumber = await GenerateReturnNumberAsync(cancellationToken).ConfigureAwait(false);

        CustomerReturn cr = new()
        {
            ReturnNumber = returnNumber, CustomerId = request.CustomerId, SalesOrderId = request.SalesOrderId,
            Status = nameof(CustomerReturnStatus.Draft), Reason = request.Reason, Notes = request.Notes,
            CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = userId
        };

        foreach (CreateCustomerReturnLineRequest lineReq in request.Lines)
        {
            CustomerReturnLine line = new() { ProductId = lineReq.ProductId, WarehouseId = lineReq.WarehouseId, LocationId = lineReq.LocationId, Quantity = lineReq.Quantity, BatchId = lineReq.BatchId, Notes = lineReq.Notes };
            cr.Lines.Add(line);
        }

        Context.CustomerReturns.Add(cr);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("CustomerReturnCreated", "CustomerReturn", cr.Id, userId, null, cancellationToken).ConfigureAwait(false);

        return await GetByIdAsync(cr.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerReturnDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        CustomerReturn? cr = await GetReturnWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (cr is null) return Result<CustomerReturnDetailDto>.Failure("RETURN_NOT_FOUND", "Customer return not found.", 404);
        CustomerReturnDetailDto dto = Mapper.Map<CustomerReturnDetailDto>(cr);
        dto = await EnrichDetailDtoAsync(dto, cr, cancellationToken).ConfigureAwait(false);
        return Result<CustomerReturnDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResponse<CustomerReturnDto>>> SearchAsync(SearchCustomerReturnsRequest request, CancellationToken cancellationToken)
    {
        IQueryable<CustomerReturn> query = BuildSearchQuery(request);
        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        IQueryable<CustomerReturn> sorted = request.SortDescending ? query.OrderByDescending(cr => cr.CreatedAtUtc) : query.OrderBy(cr => cr.CreatedAtUtc);
        List<CustomerReturn> items = await sorted
            .Include(cr => cr.SalesOrder)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<CustomerReturnDto> dtos = Mapper.Map<IReadOnlyList<CustomerReturnDto>>(items);
        dtos = await EnrichListDtosAsync(dtos, items, cancellationToken).ConfigureAwait(false);
        PaginatedResponse<CustomerReturnDto> response = new() { Items = dtos, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
        return Result<PaginatedResponse<CustomerReturnDto>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerReturnDetailDto>> ConfirmAsync(int id, int userId, CancellationToken cancellationToken)
    {
        CustomerReturn? cr = await GetReturnWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (cr is null) return Result<CustomerReturnDetailDto>.Failure("RETURN_NOT_FOUND", "Customer return not found.", 404);
        if (cr.Status == nameof(CustomerReturnStatus.Confirmed)) return Result<CustomerReturnDetailDto>.Failure("RETURN_ALREADY_CONFIRMED", "Customer return has already been confirmed.", 409);
        if (cr.Status != nameof(CustomerReturnStatus.Draft)) return Result<CustomerReturnDetailDto>.Failure("INVALID_RETURN_STATUS_TRANSITION", $"Cannot transition customer return from {cr.Status} to Confirmed.", 409);
        if (!cr.Lines.Any()) return Result<CustomerReturnDetailDto>.Failure("RETURN_MUST_HAVE_LINES", "Customer return must have at least one line.", 400);

        cr.Status = nameof(CustomerReturnStatus.Confirmed); cr.ConfirmedAtUtc = DateTime.UtcNow; cr.ConfirmedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("CustomerReturnConfirmed", "CustomerReturn", id, userId, null, cancellationToken).ConfigureAwait(false);

        CustomerReturnDetailDto dto = Mapper.Map<CustomerReturnDetailDto>(cr);
        dto = await EnrichDetailDtoAsync(dto, cr, cancellationToken).ConfigureAwait(false);
        return Result<CustomerReturnDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerReturnDetailDto>> ReceiveAsync(int id, int userId, CancellationToken cancellationToken)
    {
        CustomerReturn? cr = await GetReturnWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (cr is null) return Result<CustomerReturnDetailDto>.Failure("RETURN_NOT_FOUND", "Customer return not found.", 404);
        if (cr.Status == nameof(CustomerReturnStatus.Received)) return Result<CustomerReturnDetailDto>.Failure("RETURN_ALREADY_RECEIVED", "Customer return has already been received.", 409);
        if (cr.Status != nameof(CustomerReturnStatus.Confirmed)) return Result<CustomerReturnDetailDto>.Failure("RETURN_NOT_RECEIVABLE", "Customer return is not in Confirmed status.", 409);

        cr.Status = nameof(CustomerReturnStatus.Received); cr.ReceivedAtUtc = DateTime.UtcNow; cr.ReceivedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await PublishCustomerReturnReceivedEventAsync(cr, userId, cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("CustomerReturnReceived", "CustomerReturn", id, userId, null, cancellationToken).ConfigureAwait(false);

        CustomerReturnDetailDto dto = Mapper.Map<CustomerReturnDetailDto>(cr);
        dto = await EnrichDetailDtoAsync(dto, cr, cancellationToken).ConfigureAwait(false);
        return Result<CustomerReturnDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerReturnDetailDto>> CloseAsync(int id, int userId, CancellationToken cancellationToken)
    {
        CustomerReturn? cr = await GetReturnWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (cr is null) return Result<CustomerReturnDetailDto>.Failure("RETURN_NOT_FOUND", "Customer return not found.", 404);
        if (cr.Status != nameof(CustomerReturnStatus.Received)) return Result<CustomerReturnDetailDto>.Failure("RETURN_NOT_CLOSEABLE", "Customer return is not in Received status.", 409);

        cr.Status = nameof(CustomerReturnStatus.Closed); cr.ClosedAtUtc = DateTime.UtcNow; cr.ClosedByUserId = userId;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("CustomerReturnClosed", "CustomerReturn", id, userId, null, cancellationToken).ConfigureAwait(false);

        CustomerReturnDetailDto dto = Mapper.Map<CustomerReturnDetailDto>(cr);
        dto = await EnrichDetailDtoAsync(dto, cr, cancellationToken).ConfigureAwait(false);
        return Result<CustomerReturnDetailDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerReturnDetailDto>> CancelAsync(int id, int userId, CancellationToken cancellationToken)
    {
        CustomerReturn? cr = await GetReturnWithDetailsAsync(id, cancellationToken).ConfigureAwait(false);
        if (cr is null) return Result<CustomerReturnDetailDto>.Failure("RETURN_NOT_FOUND", "Customer return not found.", 404);
        if (cr.Status != nameof(CustomerReturnStatus.Draft) && cr.Status != nameof(CustomerReturnStatus.Confirmed))
            return Result<CustomerReturnDetailDto>.Failure("INVALID_RETURN_STATUS_TRANSITION", $"Cannot transition customer return from {cr.Status} to Cancelled.", 409);

        cr.Status = nameof(CustomerReturnStatus.Cancelled);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventService.RecordEventAsync("CustomerReturnCancelled", "CustomerReturn", id, userId, null, cancellationToken).ConfigureAwait(false);

        CustomerReturnDetailDto dto = Mapper.Map<CustomerReturnDetailDto>(cr);
        dto = await EnrichDetailDtoAsync(dto, cr, cancellationToken).ConfigureAwait(false);
        return Result<CustomerReturnDetailDto>.Success(dto);
    }

    /// <summary>
    /// Enriches a customer return detail DTO with customer name, sales order number, and
    /// per-line cross-schema display names (product code/name, warehouse name, location code).
    /// </summary>
    private async Task<CustomerReturnDetailDto> EnrichDetailDtoAsync(
        CustomerReturnDetailDto dto,
        CustomerReturn entity,
        CancellationToken cancellationToken)
    {
        string? customerName = await _lookupResolver.ResolveCustomerNameAsync(dto.CustomerId, cancellationToken).ConfigureAwait(false);
        string? salesOrderNumber = entity.SalesOrder?.OrderNumber;

        IReadOnlyCollection<int> productIds = dto.Lines.Select(l => l.ProductId).Distinct().ToArray();
        IReadOnlyCollection<int> warehouseIds = dto.Lines.Select(l => l.WarehouseId).Distinct().ToArray();
        IReadOnlyCollection<int> locationIds = dto.Lines.Where(l => l.LocationId.HasValue).Select(l => l.LocationId!.Value).Distinct().ToArray();

        IReadOnlyDictionary<int, (string Code, string Name)> products =
            await _lookupResolver.ResolveProductsAsync(productIds, cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<int, string> warehouses =
            await _lookupResolver.ResolveWarehouseNamesAsync(warehouseIds, cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<int, string> locations =
            await _lookupResolver.ResolveStorageLocationCodesAsync(locationIds, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<CustomerReturnLineDto> enrichedLines = dto.Lines
            .Select(line => EnrichLine(line, products, warehouses, locations))
            .ToArray();

        return dto with
        {
            CustomerName = customerName,
            SalesOrderNumber = salesOrderNumber,
            Lines = enrichedLines
        };
    }

    /// <summary>
    /// Returns a copy of the customer return line with cross-schema display fields hydrated.
    /// </summary>
    private static CustomerReturnLineDto EnrichLine(
        CustomerReturnLineDto line,
        IReadOnlyDictionary<int, (string Code, string Name)> products,
        IReadOnlyDictionary<int, string> warehouses,
        IReadOnlyDictionary<int, string> locations)
    {
        string? productCode = null;
        string? productName = null;
        if (products.TryGetValue(line.ProductId, out (string Code, string Name) info))
        {
            productCode = info.Code;
            productName = info.Name;
        }

        string? warehouseName = warehouses.GetValueOrDefault(line.WarehouseId);
        string? locationCode = line.LocationId.HasValue ? locations.GetValueOrDefault(line.LocationId.Value) : null;

        return line with
        {
            ProductCode = productCode,
            ProductName = productName,
            WarehouseName = warehouseName,
            LocationCode = locationCode
        };
    }

    /// <summary>
    /// Batch-resolves customer names and sales order numbers for the paginated list view.
    /// </summary>
    private async Task<IReadOnlyList<CustomerReturnDto>> EnrichListDtosAsync(
        IReadOnlyList<CustomerReturnDto> dtos,
        IReadOnlyList<CustomerReturn> entities,
        CancellationToken cancellationToken)
    {
        if (dtos.Count == 0) return dtos;

        IReadOnlyCollection<int> customerIds = dtos.Select(d => d.CustomerId).Distinct().ToArray();
        IReadOnlyDictionary<int, string> customerNames =
            await _lookupResolver.ResolveCustomerNamesAsync(customerIds, cancellationToken).ConfigureAwait(false);

        Dictionary<int, string?> soNumbersById = entities.ToDictionary(e => e.Id, e => e.SalesOrder?.OrderNumber);

        return dtos
            .Select(d => d with
            {
                CustomerName = customerNames.GetValueOrDefault(d.CustomerId),
                SalesOrderNumber = soNumbersById.GetValueOrDefault(d.Id)
            })
            .ToArray();
    }

    private async Task PublishCustomerReturnReceivedEventAsync(CustomerReturn cr, int userId, CancellationToken cancellationToken)
    {
        try
        {
            await _publishEndpoint.PublishWithCorrelationAsync(new CustomerReturnReceivedEvent
            {
                CustomerReturnId = cr.Id, CustomerReturnNumber = cr.ReturnNumber, CustomerId = cr.CustomerId,
                SalesOrderId = cr.SalesOrderId, ReceivedByUserId = userId, ReceivedAtUtc = cr.ReceivedAtUtc!.Value,
                Lines = cr.Lines.Select(l => new CustomerReturnReceivedLine { CustomerReturnLineId = l.Id, ProductId = l.ProductId, WarehouseId = l.WarehouseId, LocationId = l.LocationId, Quantity = l.Quantity, BatchId = l.BatchId }).ToList()
            }, _correlationIdAccessor, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) { Logger.Warn(ex, "Failed to publish {EventType} event", "CustomerReturnReceived"); }
    }

    private async Task<CustomerReturn?> GetReturnWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return await Context.CustomerReturns
            .Include(cr => cr.Lines)
            .Include(cr => cr.SalesOrder)
            .FirstOrDefaultAsync(cr => cr.Id == id, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GenerateReturnNumberAsync(CancellationToken cancellationToken)
    {
        string datePrefix = $"RMA-{DateTime.UtcNow:yyyyMMdd}-";
        int count = await Context.CustomerReturns.CountAsync(cr => cr.ReturnNumber.StartsWith(datePrefix), cancellationToken).ConfigureAwait(false);
        return $"{datePrefix}{(count + 1):D4}";
    }

    private IQueryable<CustomerReturn> BuildSearchQuery(SearchCustomerReturnsRequest request)
    {
        IQueryable<CustomerReturn> query = Context.CustomerReturns.AsNoTracking();
        if (request.CustomerId.HasValue) query = query.Where(cr => cr.CustomerId == request.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(cr => cr.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.ReturnNumber)) query = query.Where(cr => cr.ReturnNumber.StartsWith(request.ReturnNumber));
        if (request.SalesOrderId.HasValue) query = query.Where(cr => cr.SalesOrderId == request.SalesOrderId.Value);
        if (request.DateFrom.HasValue) query = query.Where(cr => cr.CreatedAtUtc >= request.DateFrom.Value);
        if (request.DateTo.HasValue) query = query.Where(cr => cr.CreatedAtUtc <= request.DateTo.Value);
        return query;
    }
}
