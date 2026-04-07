using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Warehouse.Common.Enums;
using Warehouse.Mapping.Profiles.Purchasing;
using Warehouse.Purchasing.DBModel;
using Warehouse.Purchasing.DBModel.Models;

namespace Warehouse.Purchasing.API.Tests.Fixtures;

/// <summary>
/// Base class for purchasing domain unit tests. Provides a fresh InMemory database and real AutoMapper per test.
/// </summary>
public abstract class PurchasingTestBase
{
    /// <summary>
    /// Gets the InMemory EF Core context for the current test.
    /// </summary>
    protected PurchasingDbContext Context { get; private set; } = null!;

    /// <summary>
    /// Gets the pre-configured AutoMapper mapper with all purchasing mapping profiles.
    /// </summary>
    protected IMapper Mapper { get; private set; } = null!;

    [SetUp]
    public virtual void SetUp()
    {
        DbContextOptions<PurchasingDbContext> options = new DbContextOptionsBuilder<PurchasingDbContext>()
            .UseInMemoryDatabase(databaseName: $"PurchasingTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        Context = new PurchasingDbContext(options);

        MapperConfiguration config = new(cfg =>
        {
            cfg.AddProfile<PurchasingMappingProfile>();
        });

        Mapper = config.CreateMapper();
    }

    [TearDown]
    public virtual void TearDown()
    {
        Context.Dispose();
    }

    /// <summary>
    /// Seeds a supplier category and returns the persisted entity.
    /// </summary>
    protected async Task<SupplierCategory> SeedCategoryAsync(string name = "Raw Materials", string? description = null)
    {
        SupplierCategory category = new()
        {
            Name = name,
            Description = description,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.SupplierCategories.Add(category);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return category;
    }

    /// <summary>
    /// Seeds an active supplier and returns the persisted entity.
    /// </summary>
    protected async Task<Supplier> SeedSupplierAsync(
        string code = "SUPP-000001",
        string name = "Test Supplier",
        string? taxId = null,
        int? categoryId = null,
        bool isDeleted = false,
        bool isActive = true)
    {
        Supplier supplier = new()
        {
            Code = code,
            Name = name,
            TaxId = taxId,
            CategoryId = categoryId,
            IsActive = isActive,
            IsDeleted = isDeleted,
            DeletedAtUtc = isDeleted ? DateTime.UtcNow : null,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return supplier;
    }

    /// <summary>
    /// Seeds a purchase order in the specified status and returns the persisted entity.
    /// </summary>
    protected async Task<PurchaseOrder> SeedPurchaseOrderAsync(
        int supplierId,
        string status = "Draft",
        int destinationWarehouseId = 1,
        int productId = 100,
        decimal orderedQuantity = 50m,
        decimal unitPrice = 10m)
    {
        PurchaseOrder po = new()
        {
            OrderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            SupplierId = supplierId,
            Status = status,
            DestinationWarehouseId = destinationWarehouseId,
            TotalAmount = orderedQuantity * unitPrice,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        PurchaseOrderLine line = new()
        {
            ProductId = productId,
            OrderedQuantity = orderedQuantity,
            UnitPrice = unitPrice,
            LineTotal = orderedQuantity * unitPrice,
            ReceivedQuantity = 0
        };

        po.Lines.Add(line);
        Context.PurchaseOrders.Add(po);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return po;
    }

    /// <summary>
    /// Seeds a purchase order with no lines and returns the persisted entity.
    /// </summary>
    protected async Task<PurchaseOrder> SeedEmptyPurchaseOrderAsync(
        int supplierId,
        string status = "Draft",
        int destinationWarehouseId = 1)
    {
        PurchaseOrder po = new()
        {
            OrderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            SupplierId = supplierId,
            Status = status,
            DestinationWarehouseId = destinationWarehouseId,
            TotalAmount = 0,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        Context.PurchaseOrders.Add(po);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return po;
    }

    /// <summary>
    /// Seeds a goods receipt with lines and returns the persisted entity.
    /// </summary>
    protected async Task<GoodsReceipt> SeedGoodsReceiptAsync(
        int purchaseOrderId,
        int purchaseOrderLineId,
        decimal receivedQuantity = 10m,
        string status = "Open",
        string inspectionStatus = "Pending",
        int warehouseId = 1)
    {
        GoodsReceipt receipt = new()
        {
            ReceiptNumber = $"GR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            PurchaseOrderId = purchaseOrderId,
            WarehouseId = warehouseId,
            Status = status,
            ReceivedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        GoodsReceiptLine line = new()
        {
            PurchaseOrderLineId = purchaseOrderLineId,
            ReceivedQuantity = receivedQuantity,
            InspectionStatus = inspectionStatus
        };

        receipt.Lines.Add(line);
        Context.GoodsReceipts.Add(receipt);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return receipt;
    }

    /// <summary>
    /// Seeds a supplier return with lines and returns the persisted entity.
    /// </summary>
    protected async Task<SupplierReturn> SeedSupplierReturnAsync(
        int supplierId,
        string status = "Draft",
        int productId = 100,
        int warehouseId = 1,
        decimal quantity = 5m)
    {
        SupplierReturn sr = new()
        {
            ReturnNumber = $"SR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            SupplierId = supplierId,
            Status = status,
            Reason = "Defective goods",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        SupplierReturnLine line = new()
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = quantity
        };

        sr.Lines.Add(line);
        Context.SupplierReturns.Add(sr);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return sr;
    }

    /// <summary>
    /// Seeds a purchase event and returns the persisted entity.
    /// </summary>
    protected async Task<PurchaseEvent> SeedPurchaseEventAsync(
        string eventType = "PurchaseOrderCreated",
        string entityType = "PurchaseOrder",
        int entityId = 1,
        int userId = 1,
        string? payload = null,
        DateTime? occurredAtUtc = null)
    {
        PurchaseEvent purchaseEvent = new()
        {
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            OccurredAtUtc = occurredAtUtc ?? DateTime.UtcNow,
            Payload = payload
        };

        Context.PurchaseEvents.Add(purchaseEvent);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return purchaseEvent;
    }
}
