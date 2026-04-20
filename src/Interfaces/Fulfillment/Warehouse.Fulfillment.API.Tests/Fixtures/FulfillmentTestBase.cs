using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Warehouse.Common.Enums;
using Warehouse.Fulfillment.DBModel;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.Mapping.Profiles.Fulfillment;

namespace Warehouse.Fulfillment.API.Tests.Fixtures;

/// <summary>
/// Base class for fulfillment domain unit tests. Provides a fresh InMemory database and real AutoMapper per test.
/// </summary>
public abstract class FulfillmentTestBase
{
    /// <summary>
    /// Gets the InMemory EF Core context for the current test.
    /// </summary>
    protected FulfillmentDbContext Context { get; private set; } = null!;

    /// <summary>
    /// Gets the pre-configured AutoMapper mapper with all fulfillment mapping profiles.
    /// </summary>
    protected IMapper Mapper { get; private set; } = null!;

    [SetUp]
    public virtual void SetUp()
    {
        DbContextOptions<FulfillmentDbContext> options = new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase(databaseName: $"FulfillmentTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        Context = new FulfillmentDbContext(options);

        MapperConfiguration config = new(cfg =>
        {
            cfg.AddProfile<FulfillmentMappingProfile>();
        });

        Mapper = config.CreateMapper();
    }

    [TearDown]
    public virtual void TearDown()
    {
        Context.Dispose();
    }

    /// <summary>
    /// Seeds a carrier and returns the persisted entity.
    /// </summary>
    protected async Task<Carrier> SeedCarrierAsync(
        string code = "DHL",
        string name = "DHL Express",
        bool isActive = true)
    {
        Carrier carrier = new()
        {
            Code = code,
            Name = name,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        Context.Carriers.Add(carrier);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return carrier;
    }

    /// <summary>
    /// Seeds a carrier service level and returns the persisted entity.
    /// </summary>
    protected async Task<CarrierServiceLevel> SeedCarrierServiceLevelAsync(
        int carrierId,
        string code = "EXPRESS",
        string name = "Express Delivery",
        int? estimatedDeliveryDays = 2,
        decimal? baseRate = 15.00m)
    {
        CarrierServiceLevel level = new()
        {
            CarrierId = carrierId,
            Code = code,
            Name = name,
            EstimatedDeliveryDays = estimatedDeliveryDays,
            BaseRate = baseRate,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.CarrierServiceLevels.Add(level);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return level;
    }

    /// <summary>
    /// Seeds a sales order in the specified status with one line and returns the persisted entity.
    /// </summary>
    protected async Task<SalesOrder> SeedSalesOrderAsync(
        string status = "Draft",
        int customerId = 1,
        int warehouseId = 1,
        int productId = 100,
        decimal orderedQuantity = 10m,
        decimal unitPrice = 25m)
    {
        SalesOrder so = new()
        {
            OrderNumber = $"SO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            CustomerId = customerId,
            CustomerAccountId = 1,
            CurrencyCode = "USD",
            Status = status,
            WarehouseId = warehouseId,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            TotalAmount = orderedQuantity * unitPrice,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        SalesOrderLine line = new()
        {
            ProductId = productId,
            OrderedQuantity = orderedQuantity,
            UnitPrice = unitPrice,
            LineTotal = orderedQuantity * unitPrice,
            PickedQuantity = 0,
            PackedQuantity = 0,
            ShippedQuantity = 0
        };

        so.Lines.Add(line);
        Context.SalesOrders.Add(so);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return so;
    }

    /// <summary>
    /// Seeds a sales order with no lines and returns the persisted entity.
    /// </summary>
    protected async Task<SalesOrder> SeedEmptySalesOrderAsync(
        string status = "Draft",
        int customerId = 1,
        int warehouseId = 1)
    {
        SalesOrder so = new()
        {
            OrderNumber = $"SO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            CustomerId = customerId,
            CustomerAccountId = 1,
            CurrencyCode = "USD",
            Status = status,
            WarehouseId = warehouseId,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            TotalAmount = 0,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        Context.SalesOrders.Add(so);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return so;
    }

    /// <summary>
    /// Seeds a pick list for the given SO and returns the persisted entity.
    /// </summary>
    protected async Task<PickList> SeedPickListAsync(
        int salesOrderId,
        int salesOrderLineId,
        int productId = 100,
        int warehouseId = 1,
        decimal requestedQuantity = 10m,
        string status = "Pending")
    {
        PickList pickList = new()
        {
            PickListNumber = $"PL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            SalesOrderId = salesOrderId,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        PickListLine line = new()
        {
            SalesOrderLineId = salesOrderLineId,
            ProductId = productId,
            WarehouseId = warehouseId,
            RequestedQuantity = requestedQuantity,
            Status = status == nameof(PickListStatus.Completed) ? nameof(PickListStatus.Completed) : nameof(PickListStatus.Pending),
            ActualQuantity = status == nameof(PickListStatus.Completed) ? requestedQuantity : null,
            PickedAtUtc = status == nameof(PickListStatus.Completed) ? DateTime.UtcNow : null,
            PickedByUserId = status == nameof(PickListStatus.Completed) ? 1 : null
        };

        pickList.Lines.Add(line);
        Context.PickLists.Add(pickList);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return pickList;
    }

    /// <summary>
    /// Seeds a parcel for the given SO and returns the persisted entity.
    /// </summary>
    protected async Task<Parcel> SeedParcelAsync(
        int salesOrderId,
        decimal? weight = 2.5m)
    {
        Parcel parcel = new()
        {
            ParcelNumber = $"PKG-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            SalesOrderId = salesOrderId,
            Weight = weight,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        Context.Parcels.Add(parcel);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return parcel;
    }

    /// <summary>
    /// Seeds a parcel item and returns the persisted entity.
    /// </summary>
    protected async Task<ParcelItem> SeedParcelItemAsync(
        int parcelId,
        int pickListLineId,
        int productId = 100,
        decimal quantity = 10m)
    {
        ParcelItem item = new()
        {
            ParcelId = parcelId,
            PickListLineId = pickListLineId,
            ProductId = productId,
            Quantity = quantity
        };

        Context.ParcelItems.Add(item);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return item;
    }

    /// <summary>
    /// Seeds a customer return with one line and returns the persisted entity.
    /// </summary>
    protected async Task<CustomerReturn> SeedCustomerReturnAsync(
        int customerId = 1,
        string status = "Draft",
        int? salesOrderId = null,
        int productId = 100,
        int warehouseId = 1,
        decimal quantity = 5m)
    {
        CustomerReturn cr = new()
        {
            ReturnNumber = $"RMA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            CustomerId = customerId,
            SalesOrderId = salesOrderId,
            Status = status,
            Reason = "Defective product",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        CustomerReturnLine line = new()
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = quantity
        };

        cr.Lines.Add(line);
        Context.CustomerReturns.Add(cr);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return cr;
    }

    /// <summary>
    /// Seeds a customer return with no lines and returns the persisted entity.
    /// </summary>
    protected async Task<CustomerReturn> SeedEmptyCustomerReturnAsync(
        int customerId = 1,
        string status = "Draft")
    {
        CustomerReturn cr = new()
        {
            ReturnNumber = $"RMA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            CustomerId = customerId,
            Status = status,
            Reason = "Defective product",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        Context.CustomerReturns.Add(cr);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return cr;
    }

    /// <summary>
    /// Seeds a fulfillment event and returns the persisted entity.
    /// </summary>
    protected async Task<FulfillmentEvent> SeedFulfillmentEventAsync(
        string eventType = "SalesOrderCreated",
        string entityType = "SalesOrder",
        int entityId = 1,
        int userId = 1,
        string? payload = null,
        DateTime? occurredAtUtc = null)
    {
        FulfillmentEvent evt = new()
        {
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            OccurredAtUtc = occurredAtUtc ?? DateTime.UtcNow,
            Payload = payload
        };

        Context.FulfillmentEvents.Add(evt);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return evt;
    }

    /// <summary>
    /// Seeds a shipment for a sales order and returns the persisted entity.
    /// </summary>
    protected async Task<Shipment> SeedShipmentAsync(
        int salesOrderId,
        string status = "Dispatched",
        int? carrierId = null)
    {
        Shipment shipment = new()
        {
            ShipmentNumber = $"SH-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            SalesOrderId = salesOrderId,
            CarrierId = carrierId,
            Status = status,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            DispatchedAtUtc = DateTime.UtcNow,
            DispatchedByUserId = 1
        };

        ShipmentTracking tracking = new()
        {
            Status = nameof(ShipmentStatus.Dispatched),
            OccurredAtUtc = DateTime.UtcNow,
            RecordedByUserId = 1
        };

        shipment.TrackingEntries.Add(tracking);
        Context.Shipments.Add(shipment);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return shipment;
    }
}
