using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Warehouse.Inventory.DBModel;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.Mapping.Profiles.Inventory;

namespace Warehouse.Inventory.API.Tests.Fixtures;

/// <summary>
/// Base class for inventory domain unit tests. Provides a fresh InMemory database and real AutoMapper per test.
/// </summary>
public abstract class InventoryTestBase
{
    /// <summary>
    /// Gets the InMemory EF Core context for the current test.
    /// </summary>
    protected InventoryDbContext Context { get; private set; } = null!;

    /// <summary>
    /// Gets the pre-configured AutoMapper mapper with all inventory mapping profiles.
    /// </summary>
    protected IMapper Mapper { get; private set; } = null!;

    [SetUp]
    public virtual void SetUp()
    {
        DbContextOptions<InventoryDbContext> options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: $"InventoryTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        Context = new InventoryDbContext(options);

        MapperConfiguration config = new(cfg =>
        {
            cfg.AddProfile<InventoryMappingProfile>();
        });

        Mapper = config.CreateMapper();
    }

    [TearDown]
    public virtual void TearDown()
    {
        Context.Dispose();
    }

    /// <summary>
    /// Seeds a product category and returns the persisted entity.
    /// </summary>
    protected async Task<ProductCategory> SeedCategoryAsync(string name = "Electronics", string? description = null)
    {
        ProductCategory category = new()
        {
            Name = name,
            Description = description,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.ProductCategories.Add(category);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return category;
    }

    /// <summary>
    /// Seeds a unit of measure and returns the persisted entity.
    /// </summary>
    protected async Task<UnitOfMeasure> SeedUnitOfMeasureAsync(string code = "PCS", string name = "Pieces")
    {
        UnitOfMeasure unit = new()
        {
            Code = code,
            Name = name,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.UnitsOfMeasure.Add(unit);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return unit;
    }

    /// <summary>
    /// Seeds a product and returns the persisted entity.
    /// </summary>
    protected async Task<Product> SeedProductAsync(
        string code = "PROD-001",
        string name = "Test Product",
        int? categoryId = null,
        int? unitOfMeasureId = null,
        bool isDeleted = false,
        bool isActive = true)
    {
        if (!unitOfMeasureId.HasValue)
        {
            UnitOfMeasure unit = await SeedUnitOfMeasureAsync($"U-{Guid.NewGuid():N}"[..10], "Auto Unit").ConfigureAwait(false);
            unitOfMeasureId = unit.Id;
        }

        Product product = new()
        {
            Code = code,
            Name = name,
            CategoryId = categoryId,
            UnitOfMeasureId = unitOfMeasureId.Value,
            IsActive = isActive,
            IsDeleted = isDeleted,
            DeletedAtUtc = isDeleted ? DateTime.UtcNow : null,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        Context.Products.Add(product);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return product;
    }

    /// <summary>
    /// Seeds a warehouse and returns the persisted entity.
    /// </summary>
    protected async Task<WarehouseEntity> SeedWarehouseAsync(
        string code = "WH-001",
        string name = "Main Warehouse",
        bool isDeleted = false)
    {
        WarehouseEntity warehouse = new()
        {
            Code = code,
            Name = name,
            IsDeleted = isDeleted,
            DeletedAtUtc = isDeleted ? DateTime.UtcNow : null,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        Context.Warehouses.Add(warehouse);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return warehouse;
    }

    /// <summary>
    /// Seeds a zone and returns the persisted entity.
    /// </summary>
    protected async Task<Zone> SeedZoneAsync(int warehouseId, string code = "Z-A", string name = "Zone A")
    {
        Zone zone = new()
        {
            WarehouseId = warehouseId,
            Code = code,
            Name = name,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.Zones.Add(zone);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return zone;
    }

    /// <summary>
    /// Seeds a storage location and returns the persisted entity.
    /// </summary>
    protected async Task<StorageLocation> SeedLocationAsync(
        int warehouseId,
        int? zoneId = null,
        string code = "LOC-001",
        string name = "Location 1",
        string locationType = "Shelf")
    {
        StorageLocation location = new()
        {
            WarehouseId = warehouseId,
            ZoneId = zoneId,
            Code = code,
            Name = name,
            LocationType = locationType,
            CreatedAtUtc = DateTime.UtcNow
        };

        Context.StorageLocations.Add(location);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return location;
    }

    /// <summary>
    /// Seeds a stock level and returns the persisted entity.
    /// </summary>
    protected async Task<StockLevel> SeedStockLevelAsync(
        int productId,
        int warehouseId,
        int? locationId = null,
        decimal quantityOnHand = 100m,
        decimal quantityReserved = 0m)
    {
        StockLevel stockLevel = new()
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            LocationId = locationId,
            QuantityOnHand = quantityOnHand,
            QuantityReserved = quantityReserved
        };

        Context.StockLevels.Add(stockLevel);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return stockLevel;
    }

    /// <summary>
    /// Seeds an inventory adjustment with lines and returns the persisted entity.
    /// </summary>
    protected async Task<InventoryAdjustment> SeedAdjustmentAsync(
        string status = "Pending",
        int? warehouseId = null,
        int? productId = null)
    {
        InventoryAdjustment adjustment = new()
        {
            Reason = "Test Adjustment",
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        if (warehouseId.HasValue && productId.HasValue)
        {
            adjustment.Lines.Add(new InventoryAdjustmentLine
            {
                ProductId = productId.Value,
                WarehouseId = warehouseId.Value,
                ExpectedQuantity = 100m,
                ActualQuantity = 95m
            });
        }

        Context.InventoryAdjustments.Add(adjustment);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return adjustment;
    }

    /// <summary>
    /// Seeds a warehouse transfer and returns the persisted entity.
    /// </summary>
    protected async Task<WarehouseTransfer> SeedTransferAsync(
        int sourceWarehouseId,
        int destinationWarehouseId,
        string status = "Draft",
        int? productId = null,
        decimal quantity = 10m)
    {
        WarehouseTransfer transfer = new()
        {
            SourceWarehouseId = sourceWarehouseId,
            DestinationWarehouseId = destinationWarehouseId,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        if (productId.HasValue)
        {
            transfer.Lines.Add(new WarehouseTransferLine
            {
                ProductId = productId.Value,
                Quantity = quantity
            });
        }

        Context.WarehouseTransfers.Add(transfer);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return transfer;
    }

    /// <summary>
    /// Seeds a stocktake session and returns the persisted entity.
    /// </summary>
    protected async Task<StocktakeSession> SeedStocktakeSessionAsync(
        int warehouseId,
        string status = "Draft",
        string name = "Test Stocktake")
    {
        StocktakeSession session = new()
        {
            WarehouseId = warehouseId,
            Name = name,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        Context.StocktakeSessions.Add(session);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return session;
    }

    /// <summary>
    /// Seeds a batch and returns the persisted entity.
    /// </summary>
    protected async Task<Batch> SeedBatchAsync(
        int productId,
        string batchNumber = "BATCH-001",
        bool isActive = true)
    {
        Batch batch = new()
        {
            ProductId = productId,
            BatchNumber = batchNumber,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        Context.Batches.Add(batch);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return batch;
    }
}
