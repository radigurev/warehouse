using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Enums;
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
    }

    /// <summary>
    /// Configures the SupplierCategory entity defaults.
    /// </summary>
    private static void ConfigureSupplierCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupplierCategory>(e =>
        {
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }

    /// <summary>
    /// Configures the Supplier entity defaults, indexes, and relationships.
    /// </summary>
    private static void ConfigureSupplier(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Supplier>(e =>
        {
            e.Property(p => p.IsActive).HasDefaultValue(true);
            e.Property(p => p.IsDeleted).HasDefaultValue(false);
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(s => s.IsDeleted)
                .HasDatabaseName("IX_Suppliers_IsDeleted")
                .HasFilter("[IsDeleted] = 0");

            e.HasIndex(s => s.TaxId)
                .HasDatabaseName("IX_Suppliers_TaxId")
                .HasFilter("[IsDeleted] = 0 AND [TaxId] IS NOT NULL");

            e.HasOne(s => s.Category)
                .WithMany(c => c.Suppliers)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    /// <summary>
    /// Configures the SupplierAddress entity defaults and relationships.
    /// </summary>
    private static void ConfigureSupplierAddress(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupplierAddress>(e =>
        {
            e.Property(p => p.IsDefault).HasDefaultValue(false);
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(a => a.Supplier)
                .WithMany(s => s.Addresses)
                .HasForeignKey(a => a.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the SupplierPhone entity defaults and relationships.
    /// </summary>
    private static void ConfigureSupplierPhone(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupplierPhone>(e =>
        {
            e.Property(p => p.IsPrimary).HasDefaultValue(false);
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(p => p.Supplier)
                .WithMany(s => s.Phones)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the SupplierEmail entity defaults and relationships.
    /// </summary>
    private static void ConfigureSupplierEmail(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupplierEmail>(e =>
        {
            e.Property(p => p.IsPrimary).HasDefaultValue(false);
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(em => em.Supplier)
                .WithMany(s => s.Emails)
                .HasForeignKey(em => em.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the PurchaseOrder entity defaults, indexes, and relationships.
    /// </summary>
    private static void ConfigurePurchaseOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrder>(e =>
        {
            e.Property(p => p.Status).HasDefaultValue("Draft");
            e.Property(p => p.TotalAmount).HasDefaultValue(0m);
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(po => po.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(po => po.SupplierId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the PurchaseOrderLine entity defaults and relationships.
    /// </summary>
    private static void ConfigurePurchaseOrderLine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrderLine>(e =>
        {
            e.Property(p => p.ReceivedQuantity).HasDefaultValue(0m);

            e.HasOne(l => l.PurchaseOrder)
                .WithMany(po => po.Lines)
                .HasForeignKey(l => l.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the GoodsReceipt entity defaults and relationships.
    /// </summary>
    private static void ConfigureGoodsReceipt(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GoodsReceipt>(e =>
        {
            e.Property(p => p.Status).HasDefaultValue("Open");
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(gr => gr.PurchaseOrder)
                .WithMany(po => po.GoodsReceipts)
                .HasForeignKey(gr => gr.PurchaseOrderId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the GoodsReceiptLine entity defaults and relationships.
    /// </summary>
    private static void ConfigureGoodsReceiptLine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GoodsReceiptLine>(e =>
        {
            e.Property(p => p.InspectionStatus).HasDefaultValue("Pending");

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
    /// Configures the SupplierReturn entity defaults and relationships.
    /// </summary>
    private static void ConfigureSupplierReturn(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupplierReturn>(e =>
        {
            e.Property(p => p.Status).HasDefaultValue("Draft");
            e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(sr => sr.Supplier)
                .WithMany()
                .HasForeignKey(sr => sr.SupplierId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the SupplierReturnLine entity relationships.
    /// </summary>
    private static void ConfigureSupplierReturnLine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupplierReturnLine>(e =>
        {
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
    /// Configures the PurchaseEvent entity defaults.
    /// </summary>
    private static void ConfigurePurchaseEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseEvent>(e =>
        {
            e.Property(p => p.OccurredAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }
}
