using Microsoft.EntityFrameworkCore;
using Warehouse.Fulfillment.DBModel.Models;

namespace Warehouse.Fulfillment.DBModel;

/// <summary>
/// EF Core database context for the fulfillment domain.
/// </summary>
public sealed class FulfillmentDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance using the specified options.
    /// </summary>
    public FulfillmentDbContext(DbContextOptions<FulfillmentDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the SalesOrders DbSet.
    /// </summary>
    public DbSet<SalesOrder> SalesOrders { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SalesOrderLines DbSet.
    /// </summary>
    public DbSet<SalesOrderLine> SalesOrderLines { get; set; } = null!;

    /// <summary>
    /// Gets or sets the PickLists DbSet.
    /// </summary>
    public DbSet<PickList> PickLists { get; set; } = null!;

    /// <summary>
    /// Gets or sets the PickListLines DbSet.
    /// </summary>
    public DbSet<PickListLine> PickListLines { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Parcels DbSet.
    /// </summary>
    public DbSet<Parcel> Parcels { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ParcelItems DbSet.
    /// </summary>
    public DbSet<ParcelItem> ParcelItems { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Shipments DbSet.
    /// </summary>
    public DbSet<Shipment> Shipments { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ShipmentLines DbSet.
    /// </summary>
    public DbSet<ShipmentLine> ShipmentLines { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ShipmentTrackingEntries DbSet.
    /// </summary>
    public DbSet<ShipmentTracking> ShipmentTrackingEntries { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Carriers DbSet.
    /// </summary>
    public DbSet<Carrier> Carriers { get; set; } = null!;

    /// <summary>
    /// Gets or sets the CarrierServiceLevels DbSet.
    /// </summary>
    public DbSet<CarrierServiceLevel> CarrierServiceLevels { get; set; } = null!;

    /// <summary>
    /// Gets or sets the CustomerReturns DbSet.
    /// </summary>
    public DbSet<CustomerReturn> CustomerReturns { get; set; } = null!;

    /// <summary>
    /// Gets or sets the CustomerReturnLines DbSet.
    /// </summary>
    public DbSet<CustomerReturnLine> CustomerReturnLines { get; set; } = null!;

    /// <summary>
    /// Gets or sets the FulfillmentEvents DbSet.
    /// </summary>
    public DbSet<FulfillmentEvent> FulfillmentEvents { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ProductPrices DbSet (Fulfillment-owned price catalog).
    /// <para>Added by CHG-FEAT-007.</para>
    /// </summary>
    public DbSet<ProductPrice> ProductPrices { get; set; } = null!;

    /// <summary>
    /// Configures entity defaults, indexes, and relationships via Fluent API.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSalesOrder(modelBuilder);
        ConfigureSalesOrderLine(modelBuilder);
        ConfigurePickList(modelBuilder);
        ConfigurePickListLine(modelBuilder);
        ConfigureParcel(modelBuilder);
        ConfigureParcelItem(modelBuilder);
        ConfigureShipment(modelBuilder);
        ConfigureShipmentLine(modelBuilder);
        ConfigureShipmentTracking(modelBuilder);
        ConfigureCarrier(modelBuilder);
        ConfigureCarrierServiceLevel(modelBuilder);
        ConfigureCustomerReturn(modelBuilder);
        ConfigureCustomerReturnLine(modelBuilder);
        ConfigureFulfillmentEvent(modelBuilder);
        ConfigureProductPrice(modelBuilder);
    }

    /// <summary>
    /// Configures the SalesOrder entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureSalesOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalesOrder>(e =>
        {
            e.ToTable("SalesOrders", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.OrderNumber).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.CustomerId).IsRequired();
            e.Property(p => p.CustomerAccountId).IsRequired();
            e.Property(p => p.CurrencyCode).IsRequired().HasMaxLength(3).HasColumnType("nvarchar(3)");
            e.Property(p => p.Status).IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)").HasDefaultValue("Draft");
            e.Property(p => p.WarehouseId).IsRequired();
            e.Property(p => p.RequestedShipDate).HasColumnType("date");
            e.Property(p => p.ShippingStreetLine1).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            e.Property(p => p.ShippingStreetLine2).HasMaxLength(200).HasColumnType("nvarchar(200)");
            e.Property(p => p.ShippingCity).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            e.Property(p => p.ShippingStateProvince).HasMaxLength(100).HasColumnType("nvarchar(100)");
            e.Property(p => p.ShippingPostalCode).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.ShippingCountryCode).IsRequired().HasMaxLength(2).HasColumnType("nvarchar(2)");
            e.Property(p => p.Notes).HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            e.Property(p => p.TotalAmount).IsRequired().HasColumnType("decimal(18,4)").HasDefaultValue(0m);
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.CreatedByUserId).IsRequired();
            e.Property(p => p.ModifiedAtUtc).HasColumnType("datetime2(7)");
            e.Property(p => p.ConfirmedAtUtc).HasColumnType("datetime2(7)");
            e.Property(p => p.ShippedAtUtc).HasColumnType("datetime2(7)");
            e.Property(p => p.CompletedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.OrderNumber)
                .IsUnique()
                .HasDatabaseName("IX_SalesOrders_OrderNumber");

            e.HasIndex(p => p.CustomerId)
                .HasDatabaseName("IX_SalesOrders_CustomerId");

            e.HasIndex(p => p.Status)
                .HasDatabaseName("IX_SalesOrders_Status");

            e.HasIndex(p => p.CreatedAtUtc)
                .HasDatabaseName("IX_SalesOrders_CreatedAtUtc");

            e.HasIndex(p => p.WarehouseId)
                .HasDatabaseName("IX_SalesOrders_WarehouseId");

            e.HasIndex(p => p.CustomerAccountId)
                .HasDatabaseName("IX_SalesOrders_CustomerAccountId");

            e.HasOne(so => so.Carrier)
                .WithMany()
                .HasForeignKey(so => so.CarrierId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(so => so.CarrierServiceLevel)
                .WithMany()
                .HasForeignKey(so => so.CarrierServiceLevelId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the SalesOrderLine entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureSalesOrderLine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalesOrderLine>(e =>
        {
            e.ToTable("SalesOrderLines", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.SalesOrderId).IsRequired();
            e.Property(p => p.ProductId).IsRequired();
            e.Property(p => p.OrderedQuantity).IsRequired().HasColumnType("decimal(18,4)");
            e.Property(p => p.UnitPrice).IsRequired().HasColumnType("decimal(18,4)");
            e.Property(p => p.LineTotal).IsRequired().HasColumnType("decimal(18,4)");
            e.Property(p => p.PickedQuantity).IsRequired().HasColumnType("decimal(18,4)").HasDefaultValue(0m);
            e.Property(p => p.PackedQuantity).IsRequired().HasColumnType("decimal(18,4)").HasDefaultValue(0m);
            e.Property(p => p.ShippedQuantity).IsRequired().HasColumnType("decimal(18,4)").HasDefaultValue(0m);
            e.Property(p => p.Notes).HasMaxLength(500).HasColumnType("nvarchar(500)");

            e.HasIndex(p => p.SalesOrderId)
                .HasDatabaseName("IX_SalesOrderLines_SalesOrderId");

            e.HasIndex(p => p.ProductId)
                .HasDatabaseName("IX_SalesOrderLines_ProductId");

            e.HasIndex(p => new { p.SalesOrderId, p.ProductId })
                .IsUnique()
                .HasDatabaseName("IX_SalesOrderLines_SOId_ProductId");

            e.HasOne(l => l.SalesOrder)
                .WithMany(so => so.Lines)
                .HasForeignKey(l => l.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the PickList entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigurePickList(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PickList>(e =>
        {
            e.ToTable("PickLists", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.PickListNumber).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.SalesOrderId).IsRequired();
            e.Property(p => p.Status).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)").HasDefaultValue("Pending");
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.CreatedByUserId).IsRequired();
            e.Property(p => p.CompletedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.PickListNumber)
                .IsUnique()
                .HasDatabaseName("IX_PickLists_PickListNumber");

            e.HasIndex(p => p.SalesOrderId)
                .HasDatabaseName("IX_PickLists_SalesOrderId");

            e.HasIndex(p => p.Status)
                .HasDatabaseName("IX_PickLists_Status");

            e.HasIndex(p => p.CreatedAtUtc)
                .HasDatabaseName("IX_PickLists_CreatedAtUtc");

            e.HasOne(pl => pl.SalesOrder)
                .WithMany(so => so.PickLists)
                .HasForeignKey(pl => pl.SalesOrderId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the PickListLine entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigurePickListLine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PickListLine>(e =>
        {
            e.ToTable("PickListLines", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.PickListId).IsRequired();
            e.Property(p => p.SalesOrderLineId).IsRequired();
            e.Property(p => p.ProductId).IsRequired();
            e.Property(p => p.WarehouseId).IsRequired();
            e.Property(p => p.RequestedQuantity).IsRequired().HasColumnType("decimal(18,4)");
            e.Property(p => p.ActualQuantity).HasColumnType("decimal(18,4)");
            e.Property(p => p.Status).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)").HasDefaultValue("Pending");
            e.Property(p => p.PickedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.PickListId)
                .HasDatabaseName("IX_PickListLines_PickListId");

            e.HasIndex(p => p.SalesOrderLineId)
                .HasDatabaseName("IX_PickListLines_SalesOrderLineId");

            e.HasIndex(p => p.ProductId)
                .HasDatabaseName("IX_PickListLines_ProductId");

            e.HasOne(l => l.PickList)
                .WithMany(pl => pl.Lines)
                .HasForeignKey(l => l.PickListId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.SalesOrderLine)
                .WithMany()
                .HasForeignKey(l => l.SalesOrderLineId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the Parcel entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureParcel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Parcel>(e =>
        {
            e.ToTable("Parcels", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.ParcelNumber).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.SalesOrderId).IsRequired();
            e.Property(p => p.Weight).HasColumnType("decimal(10,3)");
            e.Property(p => p.Length).HasColumnType("decimal(10,2)");
            e.Property(p => p.Width).HasColumnType("decimal(10,2)");
            e.Property(p => p.Height).HasColumnType("decimal(10,2)");
            e.Property(p => p.TrackingNumber).HasMaxLength(100).HasColumnType("nvarchar(100)");
            e.Property(p => p.Notes).HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.CreatedByUserId).IsRequired();
            e.Property(p => p.ModifiedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.ParcelNumber)
                .IsUnique()
                .HasDatabaseName("IX_Parcels_ParcelNumber");

            e.HasIndex(p => p.SalesOrderId)
                .HasDatabaseName("IX_Parcels_SalesOrderId");

            e.HasOne(p => p.SalesOrder)
                .WithMany(so => so.Parcels)
                .HasForeignKey(p => p.SalesOrderId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the ParcelItem entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureParcelItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParcelItem>(e =>
        {
            e.ToTable("ParcelItems", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.ParcelId).IsRequired();
            e.Property(p => p.PickListLineId).IsRequired();
            e.Property(p => p.ProductId).IsRequired();
            e.Property(p => p.Quantity).IsRequired().HasColumnType("decimal(18,4)");

            e.HasIndex(p => p.ParcelId)
                .HasDatabaseName("IX_ParcelItems_ParcelId");

            e.HasIndex(p => p.PickListLineId)
                .HasDatabaseName("IX_ParcelItems_PickListLineId");

            e.HasOne(pi => pi.Parcel)
                .WithMany(p => p.Items)
                .HasForeignKey(pi => pi.ParcelId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(pi => pi.PickListLine)
                .WithMany()
                .HasForeignKey(pi => pi.PickListLineId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the Shipment entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureShipment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipment>(e =>
        {
            e.ToTable("Shipments", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.ShipmentNumber).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.SalesOrderId).IsRequired();
            e.Property(p => p.Status).IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)").HasDefaultValue("Dispatched");
            e.Property(p => p.ShippingStreetLine1).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            e.Property(p => p.ShippingStreetLine2).HasMaxLength(200).HasColumnType("nvarchar(200)");
            e.Property(p => p.ShippingCity).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            e.Property(p => p.ShippingStateProvince).HasMaxLength(100).HasColumnType("nvarchar(100)");
            e.Property(p => p.ShippingPostalCode).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.ShippingCountryCode).IsRequired().HasMaxLength(2).HasColumnType("nvarchar(2)");
            e.Property(p => p.TrackingNumber).HasMaxLength(100).HasColumnType("nvarchar(100)");
            e.Property(p => p.TrackingUrl).HasMaxLength(500).HasColumnType("nvarchar(500)");
            e.Property(p => p.Notes).HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            e.Property(p => p.DispatchedAtUtc).IsRequired().HasColumnType("datetime2(7)");
            e.Property(p => p.DispatchedByUserId).IsRequired();

            e.HasIndex(p => p.ShipmentNumber)
                .IsUnique()
                .HasDatabaseName("IX_Shipments_ShipmentNumber");

            e.HasIndex(p => p.SalesOrderId)
                .IsUnique()
                .HasDatabaseName("IX_Shipments_SalesOrderId");

            e.HasIndex(p => p.CarrierId)
                .HasDatabaseName("IX_Shipments_CarrierId");

            e.HasIndex(p => p.Status)
                .HasDatabaseName("IX_Shipments_Status");

            e.HasIndex(p => p.DispatchedAtUtc)
                .HasDatabaseName("IX_Shipments_DispatchedAtUtc");

            e.HasOne(s => s.SalesOrder)
                .WithMany()
                .HasForeignKey(s => s.SalesOrderId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(s => s.Carrier)
                .WithMany()
                .HasForeignKey(s => s.CarrierId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(s => s.CarrierServiceLevel)
                .WithMany()
                .HasForeignKey(s => s.CarrierServiceLevelId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the ShipmentLine entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureShipmentLine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShipmentLine>(e =>
        {
            e.ToTable("ShipmentLines", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.ShipmentId).IsRequired();
            e.Property(p => p.SalesOrderLineId).IsRequired();
            e.Property(p => p.ProductId).IsRequired();
            e.Property(p => p.Quantity).IsRequired().HasColumnType("decimal(18,4)");

            e.HasIndex(p => p.ShipmentId)
                .HasDatabaseName("IX_ShipmentLines_ShipmentId");

            e.HasOne(l => l.Shipment)
                .WithMany(s => s.Lines)
                .HasForeignKey(l => l.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.SalesOrderLine)
                .WithMany()
                .HasForeignKey(l => l.SalesOrderLineId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Configures the ShipmentTracking entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureShipmentTracking(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShipmentTracking>(e =>
        {
            e.ToTable("ShipmentTrackingEntries", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.ShipmentId).IsRequired();
            e.Property(p => p.Status).IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            e.Property(p => p.Notes).HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            e.Property(p => p.OccurredAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.RecordedByUserId).IsRequired();

            e.HasIndex(p => p.ShipmentId)
                .HasDatabaseName("IX_ShipmentTrackingEntries_ShipmentId");

            e.HasOne(t => t.Shipment)
                .WithMany(s => s.TrackingEntries)
                .HasForeignKey(t => t.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the Carrier entity mapping, constraints, and indexes.
    /// </summary>
    private static void ConfigureCarrier(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Carrier>(e =>
        {
            e.ToTable("Carriers", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.Code).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.Name).IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            e.Property(p => p.ContactPhone).HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.ContactEmail).HasMaxLength(256).HasColumnType("nvarchar(256)");
            e.Property(p => p.WebsiteUrl).HasMaxLength(500).HasColumnType("nvarchar(500)");
            e.Property(p => p.TrackingUrlTemplate).HasMaxLength(500).HasColumnType("nvarchar(500)");
            e.Property(p => p.Notes).HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            e.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.CreatedByUserId).IsRequired();
            e.Property(p => p.ModifiedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.Code)
                .IsUnique()
                .HasDatabaseName("IX_Carriers_Code");

            e.HasIndex(p => p.Name)
                .HasDatabaseName("IX_Carriers_Name");
        });
    }

    /// <summary>
    /// Configures the CarrierServiceLevel entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureCarrierServiceLevel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CarrierServiceLevel>(e =>
        {
            e.ToTable("CarrierServiceLevels", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.CarrierId).IsRequired();
            e.Property(p => p.Code).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.Name).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            e.Property(p => p.BaseRate).HasColumnType("decimal(18,4)");
            e.Property(p => p.PerKgRate).HasColumnType("decimal(18,4)");
            e.Property(p => p.Notes).HasMaxLength(500).HasColumnType("nvarchar(500)");
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.ModifiedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.CarrierId)
                .HasDatabaseName("IX_CarrierServiceLevels_CarrierId");

            e.HasIndex(p => new { p.CarrierId, p.Code })
                .IsUnique()
                .HasDatabaseName("IX_CarrierServiceLevels_CarrierId_Code");

            e.HasOne(sl => sl.Carrier)
                .WithMany(c => c.ServiceLevels)
                .HasForeignKey(sl => sl.CarrierId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the CustomerReturn entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureCustomerReturn(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerReturn>(e =>
        {
            e.ToTable("CustomerReturns", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.ReturnNumber).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            e.Property(p => p.CustomerId).IsRequired();
            e.Property(p => p.Status).IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)").HasDefaultValue("Draft");
            e.Property(p => p.Reason).IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            e.Property(p => p.Notes).HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.CreatedByUserId).IsRequired();
            e.Property(p => p.ConfirmedAtUtc).HasColumnType("datetime2(7)");
            e.Property(p => p.ReceivedAtUtc).HasColumnType("datetime2(7)");
            e.Property(p => p.ClosedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => p.ReturnNumber)
                .IsUnique()
                .HasDatabaseName("IX_CustomerReturns_ReturnNumber");

            e.HasIndex(p => p.CustomerId)
                .HasDatabaseName("IX_CustomerReturns_CustomerId");

            e.HasIndex(p => p.Status)
                .HasDatabaseName("IX_CustomerReturns_Status");

            e.HasIndex(p => p.SalesOrderId)
                .HasDatabaseName("IX_CustomerReturns_SalesOrderId");

            e.HasIndex(p => p.CreatedAtUtc)
                .HasDatabaseName("IX_CustomerReturns_CreatedAtUtc");

            e.HasOne(cr => cr.SalesOrder)
                .WithMany()
                .HasForeignKey(cr => cr.SalesOrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    /// <summary>
    /// Configures the CustomerReturnLine entity mapping, constraints, indexes, and relationships.
    /// </summary>
    private static void ConfigureCustomerReturnLine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerReturnLine>(e =>
        {
            e.ToTable("CustomerReturnLines", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.CustomerReturnId).IsRequired();
            e.Property(p => p.ProductId).IsRequired();
            e.Property(p => p.WarehouseId).IsRequired();
            e.Property(p => p.Quantity).IsRequired().HasColumnType("decimal(18,4)");
            e.Property(p => p.Notes).HasMaxLength(500).HasColumnType("nvarchar(500)");

            e.HasIndex(p => p.CustomerReturnId)
                .HasDatabaseName("IX_CustomerReturnLines_CustomerReturnId");

            e.HasOne(l => l.CustomerReturn)
                .WithMany(cr => cr.Lines)
                .HasForeignKey(l => l.CustomerReturnId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the FulfillmentEvent entity mapping, constraints, and indexes.
    /// </summary>
    private static void ConfigureFulfillmentEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FulfillmentEvent>(e =>
        {
            e.ToTable("FulfillmentEvents", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.EventType).IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            e.Property(p => p.EntityType).IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            e.Property(p => p.EntityId).IsRequired();
            e.Property(p => p.UserId).IsRequired();
            e.Property(p => p.OccurredAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.Payload).HasColumnType("nvarchar(max)");

            e.HasIndex(p => p.EventType)
                .HasDatabaseName("IX_FulfillmentEvents_EventType");

            e.HasIndex(p => new { p.EntityType, p.EntityId })
                .HasDatabaseName("IX_FulfillmentEvents_EntityType_EntityId");

            e.HasIndex(p => p.OccurredAtUtc)
                .HasDatabaseName("IX_FulfillmentEvents_OccurredAtUtc");
        });
    }

    /// <summary>
    /// Configures the ProductPrice entity mapping, constraints, and indexes.
    /// <para>Added by CHG-FEAT-007. See also <see cref="ProductPrice"/>.</para>
    /// </summary>
    private static void ConfigureProductPrice(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductPrice>(e =>
        {
            e.ToTable("ProductPrices", "fulfillment");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();

            e.Property(p => p.ProductId).IsRequired();
            e.Property(p => p.CurrencyCode).IsRequired().HasMaxLength(3).HasColumnType("nvarchar(3)");
            e.Property(p => p.UnitPrice).IsRequired().HasColumnType("decimal(18,4)");
            e.Property(p => p.ValidFrom).HasColumnType("datetime2(7)");
            e.Property(p => p.ValidTo).HasColumnType("datetime2(7)");
            e.Property(p => p.CreatedAtUtc).IsRequired().HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.CreatedByUserId).IsRequired();
            e.Property(p => p.ModifiedAtUtc).HasColumnType("datetime2(7)");

            e.HasIndex(p => new { p.ProductId, p.CurrencyCode, p.ValidFrom })
                .IsUnique()
                .HasFilter(null)
                .HasDatabaseName("UX_ProductPrices_ProductId_CurrencyCode_ValidFrom");

            e.HasIndex(p => new { p.ProductId, p.CurrencyCode, p.ValidFrom, p.ValidTo })
                .HasDatabaseName("IX_ProductPrices_ProductId_CurrencyCode_ValidFrom_ValidTo");
        });
    }
}
