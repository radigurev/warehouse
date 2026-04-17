using Microsoft.EntityFrameworkCore;
using Warehouse.Nomenclature.DBModel.Models;

namespace Warehouse.Nomenclature.DBModel;

/// <summary>
/// EF Core database context for the nomenclature domain (geographic and financial reference data).
/// </summary>
public sealed class NomenclatureDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance using the specified options.
    /// </summary>
    public NomenclatureDbContext(DbContextOptions<NomenclatureDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Countries DbSet.
    /// </summary>
    public DbSet<Country> Countries { get; set; } = null!;

    /// <summary>
    /// Gets or sets the StateProvinces DbSet.
    /// </summary>
    public DbSet<StateProvince> StateProvinces { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Cities DbSet.
    /// </summary>
    public DbSet<City> Cities { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Currencies DbSet.
    /// </summary>
    public DbSet<Currency> Currencies { get; set; } = null!;

    /// <summary>
    /// Configures entity defaults, indexes, and relationships via Fluent API.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCountry(modelBuilder);
        ConfigureStateProvince(modelBuilder);
        ConfigureCity(modelBuilder);
        ConfigureCurrency(modelBuilder);
    }

    /// <summary>
    /// Configures the Country entity defaults and constraints.
    /// </summary>
    private static void ConfigureCountry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>(c =>
        {
            c.Property(e => e.IsActive).HasDefaultValue(true);
            c.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }

    /// <summary>
    /// Configures the StateProvince entity defaults, foreign keys, and constraints.
    /// </summary>
    private static void ConfigureStateProvince(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StateProvince>(sp =>
        {
            sp.Property(e => e.IsActive).HasDefaultValue(true);
            sp.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            sp.HasOne(e => e.Country)
                .WithMany(e => e.StateProvinces)
                .HasForeignKey(e => e.CountryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Configures the City entity defaults, foreign keys, and constraints.
    /// </summary>
    private static void ConfigureCity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>(ci =>
        {
            ci.Property(e => e.IsActive).HasDefaultValue(true);
            ci.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            ci.HasOne(e => e.StateProvince)
                .WithMany(e => e.Cities)
                .HasForeignKey(e => e.StateProvinceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Configures the Currency entity defaults and constraints.
    /// </summary>
    private static void ConfigureCurrency(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Currency>(cu =>
        {
            cu.Property(e => e.IsActive).HasDefaultValue(true);
            cu.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }
}
