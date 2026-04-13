using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Warehouse.Infrastructure.EntityConfiguration;

/// <summary>
/// Reusable EF Core configuration fragment for auditable entities with timestamp columns.
/// <para>Configures <c>CreatedAtUtc</c> and <c>ModifiedAtUtc</c> column defaults.</para>
/// </summary>
public static class AuditableConfigurationFragment
{
    /// <summary>
    /// Applies default timestamp configuration for auditable entities.
    /// </summary>
    public static void ApplyAuditableDefaults<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        builder.Property("CreatedAtUtc").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property("ModifiedAtUtc").IsRequired(false);
    }
}
