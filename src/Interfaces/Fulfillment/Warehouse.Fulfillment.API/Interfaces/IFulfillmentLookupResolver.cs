namespace Warehouse.Fulfillment.API.Interfaces;

/// <summary>
/// Resolves cross-schema display lookups (products, customers, warehouses, storage locations)
/// used to enrich fulfillment DTOs for the frontend. All lookups are fail-open — on error or
/// missing row, the dictionary/value is empty/null and the caller gracefully renders a fallback.
/// </summary>
public interface IFulfillmentLookupResolver
{
    /// <summary>
    /// Batch-resolves product codes and display names for the given product IDs from
    /// <c>inventory.Products</c>. Returns an empty dictionary on error or empty input.
    /// </summary>
    /// <param name="productIds">The distinct product IDs to resolve.</param>
    /// <param name="cancellationToken">Cancellation token propagated to the database call.</param>
    Task<IReadOnlyDictionary<int, (string Code, string Name)>> ResolveProductsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Batch-resolves customer display names for the given customer IDs from
    /// <c>customers.Customers</c>. Returns an empty dictionary on error or empty input.
    /// </summary>
    Task<IReadOnlyDictionary<int, string>> ResolveCustomerNamesAsync(
        IReadOnlyCollection<int> customerIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Batch-resolves warehouse display names for the given warehouse IDs from
    /// <c>inventory.Warehouses</c>. Returns an empty dictionary on error or empty input.
    /// </summary>
    Task<IReadOnlyDictionary<int, string>> ResolveWarehouseNamesAsync(
        IReadOnlyCollection<int> warehouseIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Batch-resolves storage location codes for the given location IDs from
    /// <c>inventory.StorageLocations</c>. Returns an empty dictionary on error or empty input.
    /// </summary>
    Task<IReadOnlyDictionary<int, string>> ResolveStorageLocationCodesAsync(
        IReadOnlyCollection<int> locationIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a single customer display name. Returns null when not found or on error.
    /// </summary>
    Task<string?> ResolveCustomerNameAsync(int customerId, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a single warehouse display name. Returns null when not found or on error.
    /// </summary>
    Task<string?> ResolveWarehouseNameAsync(int warehouseId, CancellationToken cancellationToken);
}
