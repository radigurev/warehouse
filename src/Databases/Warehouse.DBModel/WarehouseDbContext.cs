using Microsoft.EntityFrameworkCore;
using Warehouse.DBModel.Models.Auth;
using Warehouse.DBModel.Models.Customers;

namespace Warehouse.DBModel;

/// <summary>
/// EF Core database context for the Warehouse application.
/// </summary>
public sealed class WarehouseDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance using the specified options.
    /// </summary>
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Users DbSet.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Roles DbSet.
    /// </summary>
    public DbSet<Role> Roles { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Permissions DbSet.
    /// </summary>
    public DbSet<Permission> Permissions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UserRoles DbSet.
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; } = null!;

    /// <summary>
    /// Gets or sets the RolePermissions DbSet.
    /// </summary>
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the RefreshTokens DbSet.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UserActionLogs DbSet.
    /// </summary>
    public DbSet<UserActionLog> UserActionLogs { get; set; } = null!;

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
    /// Configures composite keys and default values that cannot be expressed via Data Annotations.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId })
            .HasName("PK_UserRoles");

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId })
            .HasName("PK_RolePermissions");

        modelBuilder.Entity<User>(u =>
        {
            u.Property(e => e.IsActive).HasDefaultValue(true);
            u.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            u.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            u.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Users_IsActive")
                .HasFilter("[IsActive] = 1");
        });

        modelBuilder.Entity<Role>(r =>
        {
            r.Property(e => e.IsSystem).HasDefaultValue(false);
            r.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Permission>(p =>
        {
            p.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<UserRole>(ur =>
        {
            ur.Property(e => e.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<RolePermission>(rp =>
        {
            rp.Property(e => e.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<RefreshToken>(rt =>
        {
            rt.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<UserActionLog>(l =>
        {
            l.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

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
    /// Configures the Customer entity defaults, filtered indexes, and cross-schema FKs.
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

            c.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            c.HasOne(e => e.ModifiedByUser)
                .WithMany()
                .HasForeignKey(e => e.ModifiedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

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
