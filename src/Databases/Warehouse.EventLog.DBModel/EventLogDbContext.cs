using Microsoft.EntityFrameworkCore;
using Warehouse.EventLog.DBModel.Models;

namespace Warehouse.EventLog.DBModel;

/// <summary>
/// EF Core database context for the centralized EventLog service.
/// Uses TPT inheritance to model domain-specific operations events.
/// <para>All event records are immutable — update and delete operations are rejected.</para>
/// </summary>
public sealed class EventLogDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance with the specified options.
    /// </summary>
    public EventLogDbContext(DbContextOptions<EventLogDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the base operations events.
    /// </summary>
    public DbSet<OperationsEvent> OperationsEvents { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Auth domain events.
    /// </summary>
    public DbSet<AuthEvent> AuthEvents { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Purchasing domain events.
    /// </summary>
    public DbSet<PurchaseEvent> PurchaseEvents { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Fulfillment domain events.
    /// </summary>
    public DbSet<FulfillmentEvent> FulfillmentEvents { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Inventory domain events.
    /// </summary>
    public DbSet<InventoryEvent> InventoryEvents { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Customer Management domain events.
    /// </summary>
    public DbSet<CustomerEvent> CustomerEvents { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureOperationsEvent(modelBuilder);
        ConfigureAuthEvent(modelBuilder);
        ConfigurePurchaseEvent(modelBuilder);
        ConfigureFulfillmentEvent(modelBuilder);
        ConfigureInventoryEvent(modelBuilder);
        ConfigureCustomerEvent(modelBuilder);
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        RejectModificationsAndDeletions();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RejectModificationsAndDeletions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        RejectModificationsAndDeletions();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RejectModificationsAndDeletions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Enforces immutability by rejecting any Modified or Deleted entity states on event entities.
    /// </summary>
    private void RejectModificationsAndDeletions()
    {
        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<OperationsEvent> entry in ChangeTracker.Entries<OperationsEvent>())
        {
            if (entry.State == EntityState.Modified)
            {
                throw new InvalidOperationException(
                    $"Operations events are immutable and cannot be modified. Event ID: {entry.Entity.Id}");
            }

            if (entry.State == EntityState.Deleted)
            {
                throw new InvalidOperationException(
                    $"Operations events are immutable and cannot be deleted. Event ID: {entry.Entity.Id}");
            }
        }
    }

    /// <summary>
    /// Configures the base OperationsEvent TPT table, columns, and indexes.
    /// </summary>
    private static void ConfigureOperationsEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OperationsEvent>(e =>
        {
            e.ToTable("OperationsEvents", "eventlog");

            e.HasKey(x => x.Id)
                .HasName("PK_OperationsEvents");

            e.Property(x => x.Id)
                .UseIdentityColumn();

            e.Property(x => x.Domain)
                .HasColumnType("nvarchar(50)")
                .IsRequired();

            e.Property(x => x.EventType)
                .HasColumnType("nvarchar(100)")
                .IsRequired();

            e.Property(x => x.EntityType)
                .HasColumnType("nvarchar(100)")
                .IsRequired();

            e.Property(x => x.EntityId)
                .IsRequired();

            e.Property(x => x.UserId)
                .IsRequired();

            e.Property(x => x.OccurredAtUtc)
                .HasColumnType("datetime2(7)")
                .IsRequired();

            e.Property(x => x.ReceivedAtUtc)
                .HasColumnType("datetime2(7)")
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .IsRequired();

            e.Property(x => x.Payload)
                .HasColumnType("nvarchar(max)");

            e.Property(x => x.CorrelationId)
                .HasColumnType("nvarchar(36)");

            e.HasIndex(x => new { x.Domain, x.OccurredAtUtc })
                .HasDatabaseName("IX_OperationsEvents_Domain_OccurredAt")
                .IsDescending(false, true);

            e.HasIndex(x => x.EventType)
                .HasDatabaseName("IX_OperationsEvents_EventType");

            e.HasIndex(x => new { x.EntityType, x.EntityId })
                .HasDatabaseName("IX_OperationsEvents_EntityType_EntityId");

            e.HasIndex(x => x.UserId)
                .HasDatabaseName("IX_OperationsEvents_UserId");

            e.HasIndex(x => x.CorrelationId)
                .HasDatabaseName("IX_OperationsEvents_CorrelationId")
                .HasFilter("[CorrelationId] IS NOT NULL");

            e.HasIndex(x => x.OccurredAtUtc)
                .HasDatabaseName("IX_OperationsEvents_OccurredAtUtc")
                .IsDescending();

            e.HasIndex(x => new { x.Domain, x.EventType, x.EntityType, x.EntityId, x.OccurredAtUtc })
                .HasDatabaseName("UQ_OperationsEvents_Dedup")
                .IsUnique();
        });
    }

    /// <summary>
    /// Configures the AuthEvent TPT derived table.
    /// </summary>
    private static void ConfigureAuthEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthEvent>(e =>
        {
            e.ToTable("AuthEvents", "eventlog");

            e.Property(x => x.Action)
                .HasColumnType("nvarchar(50)")
                .IsRequired();

            e.Property(x => x.Resource)
                .HasColumnType("nvarchar(100)")
                .IsRequired();

            e.Property(x => x.IpAddress)
                .HasColumnType("nvarchar(45)");

            e.Property(x => x.Username)
                .HasColumnType("nvarchar(50)");
        });
    }

    /// <summary>
    /// Configures the PurchaseEvent TPT derived table.
    /// </summary>
    private static void ConfigurePurchaseEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseEvent>(e =>
        {
            e.ToTable("PurchaseEvents", "eventlog");

            e.Property(x => x.SupplierName)
                .HasColumnType("nvarchar(200)");

            e.Property(x => x.DocumentNumber)
                .HasColumnType("nvarchar(50)");
        });
    }

    /// <summary>
    /// Configures the FulfillmentEvent TPT derived table.
    /// </summary>
    private static void ConfigureFulfillmentEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FulfillmentEvent>(e =>
        {
            e.ToTable("FulfillmentEvents", "eventlog");

            e.Property(x => x.CustomerName)
                .HasColumnType("nvarchar(200)");

            e.Property(x => x.DocumentNumber)
                .HasColumnType("nvarchar(50)");
        });
    }

    /// <summary>
    /// Configures the InventoryEvent TPT derived table.
    /// </summary>
    private static void ConfigureInventoryEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryEvent>(e =>
        {
            e.ToTable("InventoryEvents", "eventlog");

            e.Property(x => x.WarehouseName)
                .HasColumnType("nvarchar(200)");

            e.Property(x => x.ProductInfo)
                .HasColumnType("nvarchar(300)");
        });
    }

    /// <summary>
    /// Configures the CustomerEvent TPT derived table.
    /// </summary>
    private static void ConfigureCustomerEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerEvent>(e =>
        {
            e.ToTable("CustomerEvents", "eventlog");

            e.Property(x => x.CustomerName)
                .HasColumnType("nvarchar(200)");

            e.Property(x => x.CustomerCode)
                .HasColumnType("nvarchar(50)");
        });
    }
}
