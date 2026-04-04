using Microsoft.EntityFrameworkCore;
using Warehouse.Customers.DBModel.Models;

namespace Warehouse.Customers.DBModel;

/// <summary>
/// EF Core database context for the customers domain.
/// </summary>
public sealed class CustomersDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance using the specified options.
    /// </summary>
    public CustomersDbContext(DbContextOptions<CustomersDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the CustomerCategories DbSet.
    /// </summary>
    public DbSet<CustomerCategory> CustomerCategories { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Customers DbSet.
    /// </summary>
    public DbSet<Customer> Customers { get; set; } = null!;

    /// <summary>
    /// Gets or sets the CustomerAccounts DbSet.
    /// </summary>
    public DbSet<CustomerAccount> CustomerAccounts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the CustomerAddresses DbSet.
    /// </summary>
    public DbSet<CustomerAddress> CustomerAddresses { get; set; } = null!;

    /// <summary>
    /// Gets or sets the CustomerPhones DbSet.
    /// </summary>
    public DbSet<CustomerPhone> CustomerPhones { get; set; } = null!;

    /// <summary>
    /// Gets or sets the CustomerEmails DbSet.
    /// </summary>
    public DbSet<CustomerEmail> CustomerEmails { get; set; } = null!;

    /// <summary>
    /// Configures entity defaults, indexes, and relationships via Fluent API.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCustomerCategory(modelBuilder);
        ConfigureCustomer(modelBuilder);
        ConfigureCustomerAccount(modelBuilder);
        ConfigureCustomerAddress(modelBuilder);
        ConfigureCustomerPhone(modelBuilder);
        ConfigureCustomerEmail(modelBuilder);
    }

    /// <summary>
    /// Configures the CustomerCategory entity defaults and constraints.
    /// </summary>
    private static void ConfigureCustomerCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerCategory>(cc =>
        {
            cc.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }

    /// <summary>
    /// Configures the Customer entity defaults, filtered indexes, and audit columns.
    /// </summary>
    private static void ConfigureCustomer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(c =>
        {
            c.Property(e => e.IsActive).HasDefaultValue(true);
            c.Property(e => e.IsDeleted).HasDefaultValue(false);
            c.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            c.HasIndex(e => e.TaxId)
                .HasDatabaseName("IX_Customers_TaxId")
                .HasFilter("[IsDeleted] = 0 AND [TaxId] IS NOT NULL");

            c.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("IX_Customers_IsDeleted")
                .HasFilter("[IsDeleted] = 0");

            c.HasIndex(e => e.CreatedByUserId)
                .HasDatabaseName("IX_Customers_CreatedByUserId");

            c.HasOne(e => e.Category)
                .WithMany(e => e.Customers)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    /// <summary>
    /// Configures the CustomerAccount entity defaults and filtered unique index.
    /// </summary>
    private static void ConfigureCustomerAccount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerAccount>(ca =>
        {
            ca.Property(e => e.Balance).HasDefaultValue(0m);
            ca.Property(e => e.IsPrimary).HasDefaultValue(false);
            ca.Property(e => e.IsDeleted).HasDefaultValue(false);
            ca.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            ca.HasIndex(e => new { e.CustomerId, e.CurrencyCode })
                .IsUnique()
                .HasDatabaseName("IX_CustomerAccounts_CustomerId_CurrencyCode")
                .HasFilter("[IsDeleted] = 0");

            ca.HasOne(e => e.Customer)
                .WithMany(e => e.Accounts)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the CustomerAddress entity defaults and cascade delete.
    /// </summary>
    private static void ConfigureCustomerAddress(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerAddress>(ca =>
        {
            ca.Property(e => e.IsDefault).HasDefaultValue(false);
            ca.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            ca.HasOne(e => e.Customer)
                .WithMany(e => e.Addresses)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the CustomerPhone entity defaults and cascade delete.
    /// </summary>
    private static void ConfigureCustomerPhone(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerPhone>(cp =>
        {
            cp.Property(e => e.IsPrimary).HasDefaultValue(false);
            cp.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            cp.HasOne(e => e.Customer)
                .WithMany(e => e.Phones)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the CustomerEmail entity defaults and cascade delete.
    /// </summary>
    private static void ConfigureCustomerEmail(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerEmail>(ce =>
        {
            ce.Property(e => e.IsPrimary).HasDefaultValue(false);
            ce.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            ce.HasOne(e => e.Customer)
                .WithMany(e => e.Emails)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
