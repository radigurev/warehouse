using Microsoft.EntityFrameworkCore;
using Warehouse.Inventory.DBModel.Models;

namespace Warehouse.Inventory.DBModel;

/// <summary>
/// EF Core database context for the inventory domain.
/// </summary>
public sealed class InventoryDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance using the specified options.
    /// </summary>
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the ProductCategories DbSet.
    /// </summary>
    public DbSet<ProductCategory> ProductCategories { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UnitsOfMeasure DbSet.
    /// </summary>
    public DbSet<UnitOfMeasure> UnitsOfMeasure { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Products DbSet.
    /// </summary>
    public DbSet<Product> Products { get; set; } = null!;

    /// <summary>
    /// Gets or sets the BillOfMaterials DbSet.
    /// </summary>
    public DbSet<BillOfMaterials> BillOfMaterials { get; set; } = null!;

    /// <summary>
    /// Gets or sets the BomLines DbSet.
    /// </summary>
    public DbSet<BomLine> BomLines { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ProductAccessories DbSet.
    /// </summary>
    public DbSet<ProductAccessory> ProductAccessories { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ProductSubstitutes DbSet.
    /// </summary>
    public DbSet<ProductSubstitute> ProductSubstitutes { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Warehouses DbSet.
    /// </summary>
    public DbSet<WarehouseEntity> Warehouses { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Zones DbSet.
    /// </summary>
    public DbSet<Zone> Zones { get; set; } = null!;

    /// <summary>
    /// Gets or sets the StorageLocations DbSet.
    /// </summary>
    public DbSet<StorageLocation> StorageLocations { get; set; } = null!;

    /// <summary>
    /// Gets or sets the StockLevels DbSet.
    /// </summary>
    public DbSet<StockLevel> StockLevels { get; set; } = null!;

    /// <summary>
    /// Gets or sets the StockMovements DbSet.
    /// </summary>
    public DbSet<StockMovement> StockMovements { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Batches DbSet.
    /// </summary>
    public DbSet<Batch> Batches { get; set; } = null!;

    /// <summary>
    /// Gets or sets the InventoryAdjustments DbSet.
    /// </summary>
    public DbSet<InventoryAdjustment> InventoryAdjustments { get; set; } = null!;

    /// <summary>
    /// Gets or sets the InventoryAdjustmentLines DbSet.
    /// </summary>
    public DbSet<InventoryAdjustmentLine> InventoryAdjustmentLines { get; set; } = null!;

    /// <summary>
    /// Gets or sets the WarehouseTransfers DbSet.
    /// </summary>
    public DbSet<WarehouseTransfer> WarehouseTransfers { get; set; } = null!;

    /// <summary>
    /// Gets or sets the WarehouseTransferLines DbSet.
    /// </summary>
    public DbSet<WarehouseTransferLine> WarehouseTransferLines { get; set; } = null!;

    /// <summary>
    /// Gets or sets the StocktakeSessions DbSet.
    /// </summary>
    public DbSet<StocktakeSession> StocktakeSessions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the StocktakeCounts DbSet.
    /// </summary>
    public DbSet<StocktakeCount> StocktakeCounts { get; set; } = null!;

    /// <summary>
    /// Configures entity defaults, indexes, and relationships via Fluent API.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureProductCategory(modelBuilder);
        ConfigureUnitOfMeasure(modelBuilder);
        ConfigureProduct(modelBuilder);
        ConfigureBillOfMaterials(modelBuilder);
        ConfigureBomLine(modelBuilder);
        ConfigureProductAccessory(modelBuilder);
        ConfigureProductSubstitute(modelBuilder);
        ConfigureWarehouse(modelBuilder);
        ConfigureZone(modelBuilder);
        ConfigureStorageLocation(modelBuilder);
        ConfigureStockLevel(modelBuilder);
        ConfigureStockMovement(modelBuilder);
        ConfigureBatch(modelBuilder);
        ConfigureInventoryAdjustment(modelBuilder);
        ConfigureInventoryAdjustmentLine(modelBuilder);
        ConfigureWarehouseTransfer(modelBuilder);
        ConfigureWarehouseTransferLine(modelBuilder);
        ConfigureStocktakeSession(modelBuilder);
        ConfigureStocktakeCount(modelBuilder);
    }

    /// <summary>
    /// Configures the ProductCategory entity defaults and self-referencing relationship.
    /// </summary>
    private static void ConfigureProductCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductCategory>(e =>
        {
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(p => p.ParentCategory)
                .WithMany(p => p.ChildCategories)
                .HasForeignKey(p => p.ParentCategoryId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the UnitOfMeasure entity defaults.
    /// </summary>
    private static void ConfigureUnitOfMeasure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UnitOfMeasure>(e =>
        {
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }

    /// <summary>
    /// Configures the Product entity defaults, indexes, and relationships.
    /// </summary>
    private static void ConfigureProduct(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.Property(p => p.IsActive).HasDefaultValue(true);
            e.Property(p => p.IsDeleted).HasDefaultValue(false);
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(p => p.IsDeleted)
                .HasDatabaseName("IX_Products_IsDeleted")
                .HasFilter("[IsDeleted] = 0");

            e.HasIndex(p => p.Sku)
                .HasDatabaseName("IX_Products_Sku")
                .HasFilter("[Sku] IS NOT NULL");

            e.HasIndex(p => p.Barcode)
                .HasDatabaseName("IX_Products_Barcode")
                .HasFilter("[Barcode] IS NOT NULL");

            e.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.UnitOfMeasure)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.UnitOfMeasureId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Configures the BillOfMaterials entity defaults and relationships.
    /// </summary>
    private static void ConfigureBillOfMaterials(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BillOfMaterials>(e =>
        {
            e.Property(p => p.IsActive).HasDefaultValue(true);
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(b => b.ParentProduct)
                .WithMany()
                .HasForeignKey(b => b.ParentProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the BomLine entity relationships and unique constraint.
    /// </summary>
    private static void ConfigureBomLine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BomLine>(e =>
        {
            e.HasIndex(l => new { l.BillOfMaterialsId, l.ChildProductId })
                .IsUnique()
                .HasDatabaseName("IX_BomLines_BomId_ChildProductId");

            e.HasOne(l => l.BillOfMaterials)
                .WithMany(b => b.Lines)
                .HasForeignKey(l => l.BillOfMaterialsId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.ChildProduct)
                .WithMany()
                .HasForeignKey(l => l.ChildProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the ProductAccessory entity relationships and unique constraint.
    /// </summary>
    private static void ConfigureProductAccessory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductAccessory>(e =>
        {
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(a => new { a.ProductId, a.AccessoryProductId })
                .IsUnique()
                .HasDatabaseName("IX_ProductAccessories_ProductId_AccessoryProductId");

            e.HasIndex(a => a.ProductId)
                .HasDatabaseName("IX_ProductAccessories_ProductId");

            e.HasOne(a => a.Product)
                .WithMany(p => p.Accessories)
                .HasForeignKey(a => a.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.AccessoryProduct)
                .WithMany()
                .HasForeignKey(a => a.AccessoryProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the ProductSubstitute entity relationships and unique constraint.
    /// </summary>
    private static void ConfigureProductSubstitute(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductSubstitute>(e =>
        {
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(s => new { s.ProductId, s.SubstituteProductId })
                .IsUnique()
                .HasDatabaseName("IX_ProductSubstitutes_ProductId_SubstituteProductId");

            e.HasIndex(s => s.ProductId)
                .HasDatabaseName("IX_ProductSubstitutes_ProductId");

            e.HasOne(s => s.Product)
                .WithMany(p => p.Substitutes)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.SubstituteProduct)
                .WithMany()
                .HasForeignKey(s => s.SubstituteProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the Warehouse entity defaults and filtered index.
    /// </summary>
    private static void ConfigureWarehouse(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WarehouseEntity>(e =>
        {
            e.Property(p => p.IsActive).HasDefaultValue(true);
            e.Property(p => p.IsDeleted).HasDefaultValue(false);
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(w => w.IsDeleted)
                .HasDatabaseName("IX_Warehouses_IsDeleted")
                .HasFilter("[IsDeleted] = 0");
        });
    }

    /// <summary>
    /// Configures the Zone entity defaults and unique constraint within warehouse.
    /// </summary>
    private static void ConfigureZone(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Zone>(e =>
        {
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(z => z.WarehouseId)
                .HasDatabaseName("IX_Zones_WarehouseId");

            e.HasIndex(z => new { z.WarehouseId, z.Code })
                .IsUnique()
                .HasDatabaseName("IX_Zones_WarehouseId_Code");

            e.HasOne(z => z.Warehouse)
                .WithMany(w => w.Zones)
                .HasForeignKey(z => z.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the StorageLocation entity defaults and unique constraint within warehouse.
    /// </summary>
    private static void ConfigureStorageLocation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StorageLocation>(e =>
        {
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(l => l.WarehouseId)
                .HasDatabaseName("IX_StorageLocations_WarehouseId");

            e.HasIndex(l => l.ZoneId)
                .HasDatabaseName("IX_StorageLocations_ZoneId");

            e.HasIndex(l => new { l.WarehouseId, l.Code })
                .IsUnique()
                .HasDatabaseName("IX_StorageLocations_WarehouseId_Code");

            e.HasOne(l => l.Warehouse)
                .WithMany(w => w.Locations)
                .HasForeignKey(l => l.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.Zone)
                .WithMany(z => z.Locations)
                .HasForeignKey(l => l.ZoneId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the StockLevel entity defaults and composite unique index.
    /// </summary>
    private static void ConfigureStockLevel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockLevel>(e =>
        {
            e.Property(p => p.QuantityOnHand).HasDefaultValue(0m);
            e.Property(p => p.QuantityReserved).HasDefaultValue(0m);

            e.HasIndex(s => s.ProductId)
                .HasDatabaseName("IX_StockLevels_ProductId");

            e.HasIndex(s => s.WarehouseId)
                .HasDatabaseName("IX_StockLevels_WarehouseId");

            e.HasIndex(s => s.LocationId)
                .HasDatabaseName("IX_StockLevels_LocationId");

            e.HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(s => s.Warehouse)
                .WithMany()
                .HasForeignKey(s => s.WarehouseId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(s => s.Location)
                .WithMany()
                .HasForeignKey(s => s.LocationId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(s => s.Batch)
                .WithMany()
                .HasForeignKey(s => s.BatchId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the StockMovement entity defaults and indexes.
    /// </summary>
    private static void ConfigureStockMovement(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockMovement>(e =>
        {
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(m => m.ProductId)
                .HasDatabaseName("IX_StockMovements_ProductId");

            e.HasIndex(m => m.WarehouseId)
                .HasDatabaseName("IX_StockMovements_WarehouseId");

            e.HasIndex(m => m.CreatedAtUtc)
                .HasDatabaseName("IX_StockMovements_CreatedAtUtc");

            e.HasIndex(m => m.ReasonCode)
                .HasDatabaseName("IX_StockMovements_ReasonCode");

            e.HasOne(m => m.Product)
                .WithMany()
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(m => m.Warehouse)
                .WithMany()
                .HasForeignKey(m => m.WarehouseId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(m => m.Location)
                .WithMany()
                .HasForeignKey(m => m.LocationId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(m => m.Batch)
                .WithMany()
                .HasForeignKey(m => m.BatchId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the Batch entity defaults and unique constraint per product.
    /// </summary>
    private static void ConfigureBatch(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Batch>(e =>
        {
            e.Property(p => p.IsActive).HasDefaultValue(true);
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(b => b.ProductId)
                .HasDatabaseName("IX_Batches_ProductId");

            e.HasIndex(b => new { b.ProductId, b.BatchNumber })
                .IsUnique()
                .HasDatabaseName("IX_Batches_ProductId_BatchNumber");

            e.HasIndex(b => b.ExpiryDate)
                .HasDatabaseName("IX_Batches_ExpiryDate")
                .HasFilter("[ExpiryDate] IS NOT NULL");

            e.HasOne(b => b.Product)
                .WithMany()
                .HasForeignKey(b => b.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the InventoryAdjustment entity defaults and indexes.
    /// </summary>
    private static void ConfigureInventoryAdjustment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryAdjustment>(e =>
        {
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.Status).HasDefaultValue("Pending");

            e.HasIndex(a => a.Status)
                .HasDatabaseName("IX_InventoryAdjustments_Status");

            e.HasIndex(a => a.CreatedAtUtc)
                .HasDatabaseName("IX_InventoryAdjustments_CreatedAtUtc");
        });
    }

    /// <summary>
    /// Configures the InventoryAdjustmentLine entity relationships.
    /// </summary>
    private static void ConfigureInventoryAdjustmentLine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryAdjustmentLine>(e =>
        {
            e.HasIndex(l => l.AdjustmentId)
                .HasDatabaseName("IX_InventoryAdjustmentLines_AdjustmentId");

            e.HasOne(l => l.Adjustment)
                .WithMany(a => a.Lines)
                .HasForeignKey(l => l.AdjustmentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(l => l.Warehouse)
                .WithMany()
                .HasForeignKey(l => l.WarehouseId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(l => l.Location)
                .WithMany()
                .HasForeignKey(l => l.LocationId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(l => l.Batch)
                .WithMany()
                .HasForeignKey(l => l.BatchId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the WarehouseTransfer entity relationships and indexes.
    /// </summary>
    private static void ConfigureWarehouseTransfer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WarehouseTransfer>(e =>
        {
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.Status).HasDefaultValue("Draft");

            e.HasIndex(t => t.SourceWarehouseId)
                .HasDatabaseName("IX_WarehouseTransfers_SourceWarehouseId");

            e.HasIndex(t => t.DestinationWarehouseId)
                .HasDatabaseName("IX_WarehouseTransfers_DestinationWarehouseId");

            e.HasIndex(t => t.Status)
                .HasDatabaseName("IX_WarehouseTransfers_Status");

            e.HasOne(t => t.SourceWarehouse)
                .WithMany()
                .HasForeignKey(t => t.SourceWarehouseId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(t => t.DestinationWarehouse)
                .WithMany()
                .HasForeignKey(t => t.DestinationWarehouseId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the WarehouseTransferLine entity relationships.
    /// </summary>
    private static void ConfigureWarehouseTransferLine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WarehouseTransferLine>(e =>
        {
            e.HasIndex(l => l.TransferId)
                .HasDatabaseName("IX_WarehouseTransferLines_TransferId");

            e.HasOne(l => l.Transfer)
                .WithMany(t => t.Lines)
                .HasForeignKey(l => l.TransferId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(l => l.SourceLocation)
                .WithMany()
                .HasForeignKey(l => l.SourceLocationId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(l => l.DestinationLocation)
                .WithMany()
                .HasForeignKey(l => l.DestinationLocationId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the StocktakeSession entity defaults and indexes.
    /// </summary>
    private static void ConfigureStocktakeSession(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StocktakeSession>(e =>
        {
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.Status).HasDefaultValue("Draft");

            e.HasIndex(s => s.WarehouseId)
                .HasDatabaseName("IX_StocktakeSessions_WarehouseId");

            e.HasIndex(s => s.Status)
                .HasDatabaseName("IX_StocktakeSessions_Status");

            e.HasIndex(s => s.CreatedAtUtc)
                .HasDatabaseName("IX_StocktakeSessions_CreatedAtUtc");

            e.HasOne(s => s.Warehouse)
                .WithMany()
                .HasForeignKey(s => s.WarehouseId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(s => s.Zone)
                .WithMany()
                .HasForeignKey(s => s.ZoneId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the StocktakeCount entity relationships and unique constraint.
    /// </summary>
    private static void ConfigureStocktakeCount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StocktakeCount>(e =>
        {
            e.Property(p => p.ExpectedQuantity).HasDefaultValue(0m);
            e.Property(p => p.ActualQuantity).HasDefaultValue(0m);
            e.Property(p => p.Variance).HasDefaultValue(0m);

            e.HasIndex(c => c.SessionId)
                .HasDatabaseName("IX_StocktakeCounts_SessionId");

            e.HasIndex(c => c.ProductId)
                .HasDatabaseName("IX_StocktakeCounts_ProductId");

            e.HasIndex(c => new { c.SessionId, c.ProductId, c.LocationId })
                .IsUnique()
                .HasDatabaseName("IX_StocktakeCounts_SessionId_ProductId_LocationId");

            e.HasOne(c => c.Session)
                .WithMany(s => s.Counts)
                .HasForeignKey(c => c.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(c => c.Location)
                .WithMany()
                .HasForeignKey(c => c.LocationId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
