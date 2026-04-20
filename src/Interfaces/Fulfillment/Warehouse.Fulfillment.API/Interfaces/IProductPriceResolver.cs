using Warehouse.Fulfillment.DBModel.Models;

namespace Warehouse.Fulfillment.API.Interfaces;

/// <summary>
/// Resolves the effective <see cref="ProductPrice"/> for a (product, currency, at-date) tuple.
/// <para>Conforms to CHG-FEAT-007 §2.3 Price Resolution and §2.5 Diagnostic Resolver Endpoint.</para>
/// </summary>
public interface IProductPriceResolver
{
    /// <summary>
    /// Resolves the best-match active <see cref="ProductPrice"/> for the given inputs, or null when
    /// no catalog row covers the tuple. Tiebreaker: most recent concrete <c>ValidFrom</c>; rows with
    /// <c>ValidFrom = null</c> rank below any concrete date.
    /// </summary>
    /// <param name="productId">The product ID to look up.</param>
    /// <param name="currencyCode">The ISO 4217 currency code.</param>
    /// <param name="onUtc">The reference point in UTC for validity checks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved <see cref="ProductPrice"/>, or null when no match exists.</returns>
    Task<ProductPrice?> ResolveAsync(
        int productId,
        string currencyCode,
        DateTime onUtc,
        CancellationToken cancellationToken);
}
