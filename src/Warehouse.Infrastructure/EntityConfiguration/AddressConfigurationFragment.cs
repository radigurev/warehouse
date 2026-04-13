using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Warehouse.Infrastructure.EntityConfiguration;

/// <summary>
/// Reusable EF Core configuration fragment for address-bearing entities.
/// <para>Configures standard address columns: AddressLine1, AddressLine2, City, Region, PostalCode, Country, IsDefault.</para>
/// </summary>
public static class AddressConfigurationFragment
{
    /// <summary>
    /// Applies default address column configuration for address entities.
    /// </summary>
    public static void ApplyAddressDefaults<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        builder.Property("AddressLine1").HasMaxLength(200).IsRequired();
        builder.Property("AddressLine2").HasMaxLength(200).IsRequired(false);
        builder.Property("City").HasMaxLength(100).IsRequired();
        builder.Property("Region").HasMaxLength(100).IsRequired(false);
        builder.Property("PostalCode").HasMaxLength(20).IsRequired(false);
        builder.Property("Country").HasMaxLength(100).IsRequired();
        builder.Property("IsDefault").HasDefaultValue(false);
    }
}
