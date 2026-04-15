using Microsoft.EntityFrameworkCore;
using Warehouse.Purchasing.DBModel.Models;

namespace Warehouse.Purchasing.DBModel;

/// <summary>
/// EF Core database context for the purchasing domain.
/// </summary>
public sealed class PurchasingDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance using the specified options.
    /// </summary>
    public PurchasingDbContext(DbContextOptions<PurchasingDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the SupplierCategories DbSet.
    /// </summary>
    public DbSet<SupplierCategory> SupplierCategories { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Suppliers DbSet.
    /// </summary>
    public DbSet<Supplier> Suppliers { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SupplierAddresses DbSet.
    /// </summary>
    public DbSet<SupplierAddress> SupplierAddresses { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SupplierPhones DbSet.
    /// </summary>
    public DbSet<SupplierPhone> SupplierPhones { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SupplierEmails DbSet.
    /// </summary>
    public DbSet<SupplierEmail> SupplierEmails { get; set; } = null!;

    /// <summary>
    /// Gets or sets the PurchaseOrders DbSet.
    /// </summary>
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;

    /// <summary>
    /// Gets or sets the PurchaseOrderLines DbSet.
    /// </summary>
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; } = null!;

    /// <summary>
    /// Gets or sets the GoodsReceipts DbSet.
    /// </summary>
    public DbSet<GoodsReceipt> GoodsReceipts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the GoodsReceiptLines DbSet.
    /// </summary>
    public DbSet<GoodsReceiptLine> GoodsReceiptLines { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SupplierReturns DbSet.
    /// </summary>
    public DbSet<SupplierReturn> SupplierReturns { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SupplierReturnLines DbSet.
    /// </summary>
    public DbSet<SupplierReturnLine> SupplierReturnLines { get; set; } = null!;

    /// <summary>
    /// Gets or sets the PurchaseEvents DbSet.
    /// </summary>
    public DbSet<PurchaseEvent> PurchaseEvents { get; set; } = null!;

    /// <summary>
    /// Gets or sets the read-only product lookup set (mapped to inventory.Products via ToView).
    /// </summary>
    public DbSet<ProductLookup> ProductLookups { get; set; } = null!;

    /// <summary>
    /// Configures entity defaults, indexes, and relationships via Fluent API.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSupplierCategory(modelBuilder);
        ConfigureSupplier(modelBuilder);
        ConfigureSupplierAddress(modelBuilder);
        ConfigureSupplierPhone(modelBuilder);
        ConfigureSupplierEmail(modelBuilder);
        ConfigurePurchaseOrder(modelBuilder);
        ConfigurePurchaseOrderLine(modelBuilder);
        ConfigureGoodsReceipt(modelBuilder);
        ConfigureGoodsReceiptLine(modelBuilder);
        ConfigureSupplierReturn(modelBuilder);
        ConfigureSupplierReturnLine(modelBuilder);
        ConfigurePurchaseEvent(modelBuilder);
        ConfigureProductLookup(modelBuilder);
    }

    /// <summary>
    /// Configures the SupplierCategory entity mapping and constraints.
    /// </summary>
    private static void ConfigureSupplierCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupplierCategory>(e =>
        {
            e.ToTable("SupplierCategories", "purchasing");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.Name).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            e.Property(p => p.Description).HasMaxLength(500).HasColumnType("nvarchar(500)");
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.ModifiedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.Name)
                .IsUnique()
                .HasDatabaseName("IX_SupplierCategories_Name");
        });
    }

    /// <summary>
    /// Configures the Supplier entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureSupplier(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Supplier>(e =>
        {
            e.ToTable("Suppliers", "purchasing");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.Code).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.Name).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            e.Property(p => p.TaxId).HasMaxLength(50).HasColumnType("nvarchar(50)");
            e.Property(p => p.Notes).HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            e.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);
            e.Property(p => p.IsDeleted).IsRequired().HasDefaultValue(false);
            e.Property(p => p.DeletedAtUtc).HasColumnType("datetime2(7)");
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.CreatedByUserId).IsRequired();
            e.Property(p => p.ModifiedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.Code)
                .IsUnique()
                .HasDatabaseName("IX_Suppliers_Code");

            e.HasIndex(p => p.CategoryId)
                .HasDatabaseName("IX_Suppliers_CategoryId");

            e.HasIndex(p => p.Name)
                .HasDatabaseName("IX_Suppliers_Name");

            e.HasIndex(p => p.IsDeleted)
                .HasDatabaseName("IX_Suppliers_IsDeleted")
                .HasFilter("[IsDeleted] = 0");

            e.HasIndex(p => p.TaxId)
                .HasDatabaseName("IX_Suppliers_TaxId")
                .HasFilter("[IsDeleted] = 0 AND [TaxId] IS NOT NULL");

            e.HasOne(s => s.Category)
                .WithMany(c => c.Suppliers)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    /// <summary>
    /// Configures the SupplierAddress entity mapping, constraints, and relationships.
    /// </summary>
    private static void ConfigureSupplierAddress(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupplierAddress>(e =>
        {
            e.ToTable("SupplierAddresses", "purchasing");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.SupplierId).IsRequired();
            e.Property(p => p.AddressType).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.StreetLine1).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            e.Property(p => p.StreetLine2).HasMaxLength(200).HasColumnType("nvarchar(200)");
            e.Property(p => p.City).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            e.Property(p => p.StateProvince).HasMaxLength(100).HasColumnType("nvarchar(100)");
            e.Property(p => p.PostalCode).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.CountryCode).IsRequired().HasMaxLength(2).HasColumnType("nvarchar(2)");
            e.Property(p => p.IsDefault).IsRequired().HasDefaultValue(false);
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.ModifiedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.SupplierId)
                .HasDatabaseName("IX_SupplierAddresses_SupplierId");

            e.HasOne(a => a.Supplier)
                .WithMany(s => s.Addresses)
                .HasForeignKey(a => a.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the SupplierPhone entity mapping, constraints, and relationships.
    /// </summary>
    private static void ConfigureSupplierPhone(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupplierPhone>(e =>
        {
            e.ToTable("SupplierPhones", "purchasing");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.SupplierId).IsRequired();
            e.Property(p => p.PhoneType).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.PhoneNumber).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.Extension).HasMaxLength(10).HasColumnType("nvarchar(10)");
            e.Property(p => p.IsPrimary).IsRequired().HasDefaultValue(false);
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.ModifiedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.SupplierId)
                .HasDatabaseName("IX_SupplierPhones_SupplierId");

            e.HasOne(p => p.Supplier)
                .WithMany(s => s.Phones)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the SupplierEmail entity mapping, constraints, and relationships.
    /// </summary>
    private static void ConfigureSupplierEmail(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupplierEmail>(e =>
        {
            e.ToTable("SupplierEmails", "purchasing");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.SupplierId).IsRequired();
            e.Property(p => p.EmailType).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.EmailAddress).IsRequired().HasMaxLength(256).HasColumnType("nvarchar(256)");
            e.Property(p => p.IsPrimary).IsRequired().HasDefaultValue(false);
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.ModifiedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.SupplierId)
                .HasDatabaseName("IX_SupplierEmails_SupplierId");

            e.HasIndex(p => new { p.SupplierId, p.EmailAddress })
                .IsUnique()
                .HasDatabaseName("IX_SupplierEmails_SupplierId_EmailAddress");

            e.HasOne(em => em.Supplier)
                .WithMany(s => s.Emails)
                .HasForeignKey(em => em.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the PurchaseOrder entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigurePurchaseOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrder>(e =>
        {
            e.ToTable("PurchaseOrders", "purchasing");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.OrderNumber).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.SupplierId).IsRequired();
            e.Property(p => p.Status).IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)").HasDefaultValue("Draft");
            e.Property(p => p.DestinationWarehouseId).IsRequired();
            e.Property(p => p.ExpectedDeliveryDate).HasColumnType("date");
            e.Property(p => p.Notes).HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            e.Property(p => p.TotalAmount).IsRequired().HasColumnType("decimal(18,4)").HasDefaultValue(0m);
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.CreatedByUserId).IsRequired();
            e.Property(p => p.ModifiedAtUtc).HasColumnType("datetime2(7)");
            e.Property(p => p.ConfirmedAtUtc).HasColumnType("datetime2(7)");
            e.Property(p => p.ClosedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.OrderNumber)
                .IsUnique()
                .HasDatabaseName("IX_PurchaseOrders_OrderNumber");

            e.HasIndex(p => p.SupplierId)
                .HasDatabaseName("IX_PurchaseOrders_SupplierId");

            e.HasIndex(p => p.Status)
                .HasDatabaseName("IX_PurchaseOrders_Status");

            e.HasIndex(p => p.CreatedAtUtc)
                .HasDatabaseName("IX_PurchaseOrders_CreatedAtUtc");

            e.HasIndex(p => p.DestinationWarehouseId)
                .HasDatabaseName("IX_PurchaseOrders_DestinationWarehouseId");

            e.HasOne(po => po.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(po => po.SupplierId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the PurchaseOrderLine entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigurePurchaseOrderLine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrderLine>(e =>
        {
            e.ToTable("PurchaseOrderLines", "purchasing");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.PurchaseOrderId).IsRequired();
            e.Property(p => p.ProductId).IsRequired();
            e.Property(p => p.OrderedQuantity).IsRequired().HasColumnType("decimal(18,4)");
            e.Property(p => p.UnitPrice).IsRequired().HasColumnType("decimal(18,4)");
            e.Property(p => p.LineTotal).IsRequired().HasColumnType("decimal(18,4)");
            e.Property(p => p.ReceivedQuantity).IsRequired().HasColumnType("decimal(18,4)").HasDefaultValue(0m);
            e.Property(p => p.Notes).HasMaxLength(500).HasColumnType("nvarchar(500)");

            e.HasIndex(p => p.PurchaseOrderId)
                .HasDatabaseName("IX_PurchaseOrderLines_PurchaseOrderId");

            e.HasIndex(p => p.ProductId)
                .HasDatabaseName("IX_PurchaseOrderLines_ProductId");

            e.HasIndex(p => new { p.PurchaseOrderId, p.ProductId })
                .IsUnique()
                .HasDatabaseName("IX_PurchaseOrderLines_POId_ProductId");

            e.HasOne(l => l.PurchaseOrder)
                .WithMany(po => po.Lines)
                .HasForeignKey(l => l.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the GoodsReceipt entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureGoodsReceipt(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GoodsReceipt>(e =>
        {
            e.ToTable("GoodsReceipts", "purchasing");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.ReceiptNumber).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.PurchaseOrderId).IsRequired();
            e.Property(p => p.WarehouseId).IsRequired();
            e.Property(p => p.Status).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)").HasDefaultValue("Open");
            e.Property(p => p.Notes).HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            e.Property(p => p.ReceivedAtUtc).IsRequired().HasColumnType("datetime2(7)");
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.CreatedByUserId).IsRequired();
            e.Property(p => p.CompletedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.ReceiptNumber)
                .IsUnique()
                .HasDatabaseName("IX_GoodsReceipts_ReceiptNumber");

            e.HasIndex(p => p.PurchaseOrderId)
                .HasDatabaseName("IX_GoodsReceipts_PurchaseOrderId");

            e.HasIndex(p => p.WarehouseId)
                .HasDatabaseName("IX_GoodsReceipts_WarehouseId");

            e.HasIndex(p => p.ReceivedAtUtc)
                .HasDatabaseName("IX_GoodsReceipts_ReceivedAtUtc");

            e.HasOne(gr => gr.PurchaseOrder)
                .WithMany(po => po.GoodsReceipts)
                .HasForeignKey(gr => gr.PurchaseOrderId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the GoodsReceiptLine entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureGoodsReceiptLine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GoodsReceiptLine>(e =>
        {
            e.ToTable("GoodsReceiptLines", "purchasing");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.GoodsReceiptId).IsRequired();
            e.Property(p => p.PurchaseOrderLineId).IsRequired();
            e.Property(p => p.ReceivedQuantity).IsRequired().HasColumnType("decimal(18,4)");
            e.Property(p => p.BatchNumber).HasMaxLength(50).HasColumnType("nvarchar(50)");
            e.Property(p => p.ManufacturingDate).HasColumnType("date");
            e.Property(p => p.ExpiryDate).HasColumnType("date");
            e.Property(p => p.InspectionStatus).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)").HasDefaultValue("Pending");
            e.Property(p => p.InspectionNote).HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            e.Property(p => p.InspectedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.GoodsReceiptId)
                .HasDatabaseName("IX_GoodsReceiptLines_GoodsReceiptId");

            e.HasIndex(p => p.PurchaseOrderLineId)
                .HasDatabaseName("IX_GoodsReceiptLines_PurchaseOrderLineId");

            e.HasOne(l => l.GoodsReceipt)
                .WithMany(gr => gr.Lines)
                .HasForeignKey(l => l.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.PurchaseOrderLine)
                .WithMany(pol => pol.GoodsReceiptLines)
                .HasForeignKey(l => l.PurchaseOrderLineId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the SupplierReturn entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureSupplierReturn(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupplierReturn>(e =>
        {
            e.ToTable("SupplierReturns", "purchasing");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.ReturnNumber).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.SupplierId).IsRequired();
            e.Property(p => p.Status).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)").HasDefaultValue("Draft");
            e.Property(p => p.Reason).IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            e.Property(p => p.Notes).HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.CreatedByUserId).IsRequired();
            e.Property(p => p.ConfirmedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.ReturnNumber)
                .IsUnique()
                .HasDatabaseName("IX_SupplierReturns_ReturnNumber");

            e.HasIndex(p => p.SupplierId)
                .HasDatabaseName("IX_SupplierReturns_SupplierId");

            e.HasIndex(p => p.Status)
                .HasDatabaseName("IX_SupplierReturns_Status");

            e.HasOne(sr => sr.Supplier)
                .WithMany()
                .HasForeignKey(sr => sr.SupplierId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the SupplierReturnLine entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureSupplierReturnLine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupplierReturnLine>(e =>
        {
            e.ToTable("SupplierReturnLines", "purchasing");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.SupplierReturnId).IsRequired();
            e.Property(p => p.ProductId).IsRequired();
            e.Property(p => p.WarehouseId).IsRequired();
            e.Property(p => p.Quantity).IsRequired().HasColumnType("decimal(18,4)");
            e.Property(p => p.Notes).HasMaxLength(500).HasColumnType("nvarchar(500)");

            e.HasIndex(p => p.SupplierReturnId)
                .HasDatabaseName("IX_SupplierReturnLines_SupplierReturnId");

            e.HasOne(l => l.SupplierReturn)
                .WithMany(sr => sr.Lines)
                .HasForeignKey(l => l.SupplierReturnId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.GoodsReceiptLine)
                .WithMany()
                .HasForeignKey(l => l.GoodsReceiptLineId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the PurchaseEvent entity mapping, constraints, and indexes.
    /// </summary>
    private static void ConfigurePurchaseEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseEvent>(e =>
        {
            e.ToTable("PurchaseEvents", "purchasing");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.EventType).IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            e.Property(p => p.EntityType).IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            e.Property(p => p.EntityId).IsRequired();
            e.Property(p => p.UserId).IsRequired();
            e.Property(p => p.OccurredAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.Payload).HasColumnType("nvarchar(max)");

            e.HasIndex(p => p.EventType)
                .HasDatabaseName("IX_PurchaseEvents_EventType");

            e.HasIndex(p => new { p.EntityType, p.EntityId })
                .HasDatabaseName("IX_PurchaseEvents_EntityType_EntityId");

            e.HasIndex(p => p.OccurredAtUtc)
                .HasDatabaseName("IX_PurchaseEvents_OccurredAtUtc");
        });
    }

    /// <summary>
    /// Configures the read-only ProductLookup entity mapped to inventory.Products (no migrations).
    /// </summary>
    private static void ConfigureProductLookup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductLookup>(e =>
        {
            e.ToView("Products", "inventory");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(200).HasColumnType("nvarchar(200)");
            e.Property(p => p.Code).HasMaxLength(50).HasColumnType("nvarchar(50)");
        });
    }
}
