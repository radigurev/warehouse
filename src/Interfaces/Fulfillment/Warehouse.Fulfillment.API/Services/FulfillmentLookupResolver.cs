using Microsoft.EntityFrameworkCore;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Fulfillment.DBModel;

namespace Warehouse.Fulfillment.API.Services;

/// <summary>
/// Implements cross-schema display lookups against <c>customers.Customers</c>,
/// <c>inventory.Warehouses</c>, <c>inventory.Products</c>, and
/// <c>inventory.StorageLocations</c> using raw SQL queries (EF cannot traverse DbContexts).
/// All methods are fail-open: any exception or empty input yields an empty result.
/// </summary>
public sealed class FulfillmentLookupResolver : IFulfillmentLookupResolver
{
    private readonly FulfillmentDbContext _context;

    /// <summary>
    /// Initializes a new instance using the fulfillment database context, which also hosts
    /// the connection used for cross-schema raw queries into the customers and inventory schemas.
    /// </summary>
    public FulfillmentLookupResolver(FulfillmentDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, (string Code, string Name)>> ResolveProductsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return new Dictionary<int, (string Code, string Name)>();

        string idList = string.Join(",", productIds);
        try
        {
#pragma warning disable EF1002 // idList is built from trusted int primary keys -- SQL injection not possible
            List<ProductLookupRow> rows = await _context.Database
                .SqlQueryRaw<ProductLookupRow>($"SELECT Id, Code, Name FROM inventory.Products WHERE Id IN ({idList})")
                .ToListAsync(cancellationToken).ConfigureAwait(false);
#pragma warning restore EF1002
            return rows.ToDictionary(r => r.Id, r => (r.Code, r.Name));
        }
        catch
        {
            return new Dictionary<int, (string Code, string Name)>();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, string>> ResolveCustomerNamesAsync(
        IReadOnlyCollection<int> customerIds,
        CancellationToken cancellationToken)
    {
        return await ResolveIdNameAsync(customerIds, "customers.Customers", "Name", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, string>> ResolveWarehouseNamesAsync(
        IReadOnlyCollection<int> warehouseIds,
        CancellationToken cancellationToken)
    {
        return await ResolveIdNameAsync(warehouseIds, "inventory.Warehouses", "Name", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, string>> ResolveStorageLocationCodesAsync(
        IReadOnlyCollection<int> locationIds,
        CancellationToken cancellationToken)
    {
        return await ResolveIdNameAsync(locationIds, "inventory.StorageLocations", "Code", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> ResolveCustomerNameAsync(int customerId, CancellationToken cancellationToken)
    {
        return await ResolveSingleScalarAsync("customers.Customers", "Name", customerId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> ResolveWarehouseNameAsync(int warehouseId, CancellationToken cancellationToken)
    {
        return await ResolveSingleScalarAsync("inventory.Warehouses", "Name", warehouseId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Batch scalar-column lookup helper shared by customer/warehouse/location resolvers.
    /// </summary>
    private async Task<IReadOnlyDictionary<int, string>> ResolveIdNameAsync(
        IReadOnlyCollection<int> ids,
        string table,
        string column,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return new Dictionary<int, string>();
        string idList = string.Join(",", ids);
        try
        {
#pragma warning disable EF1002 // idList is built from trusted int primary keys -- SQL injection not possible
            List<IdNameRow> rows = await _context.Database
                .SqlQueryRaw<IdNameRow>($"SELECT Id, {column} AS Name FROM {table} WHERE Id IN ({idList})")
                .ToListAsync(cancellationToken).ConfigureAwait(false);
#pragma warning restore EF1002
            return rows.ToDictionary(r => r.Id, r => r.Name);
        }
        catch
        {
            return new Dictionary<int, string>();
        }
    }

    /// <summary>
    /// Single-row scalar lookup helper used by the convenience name resolvers.
    /// </summary>
    private async Task<string?> ResolveSingleScalarAsync(
        string table,
        string column,
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
#pragma warning disable EF1002 // id is a trusted int primary key -- SQL injection not possible
            List<string> rows = await _context.Database
                .SqlQueryRaw<string>($"SELECT TOP 1 {column} AS [Value] FROM {table} WHERE Id = {id}")
                .ToListAsync(cancellationToken).ConfigureAwait(false);
#pragma warning restore EF1002
            return rows.Count == 0 ? null : rows[0];
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Keyless projection type used by <see cref="ResolveProductsAsync"/>.</summary>
    private sealed class ProductLookupRow
    {
        /// <summary>Gets or sets the product primary key.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the product code.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Gets or sets the product display name.</summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Keyless (Id, Name) projection used by scalar-column batch lookups.</summary>
    private sealed class IdNameRow
    {
        /// <summary>Gets or sets the row primary key.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the display value (Name or Code column).</summary>
        public string Name { get; set; } = string.Empty;
    }
}
