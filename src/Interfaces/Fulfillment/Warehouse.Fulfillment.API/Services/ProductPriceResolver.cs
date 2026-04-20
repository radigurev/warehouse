using Microsoft.EntityFrameworkCore;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Fulfillment.DBModel;
using Warehouse.Fulfillment.DBModel.Models;

namespace Warehouse.Fulfillment.API.Services;

/// <summary>
/// Resolves the effective product price per CHG-FEAT-007 §2.3.
/// Returns the row with the most recent concrete <c>ValidFrom</c> that covers <c>onUtc</c>;
/// rows with <c>ValidFrom IS NULL</c> rank below any concrete date.
/// <para>See <see cref="IProductPriceResolver"/>, <see cref="FulfillmentDbContext"/>.</para>
/// </summary>
public sealed class ProductPriceResolver : IProductPriceResolver
{
    private readonly FulfillmentDbContext _context;

    /// <summary>
    /// Initializes a new instance with the specified Fulfillment database context.
    /// </summary>
    public ProductPriceResolver(FulfillmentDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ProductPrice?> ResolveAsync(
        int productId,
        string currencyCode,
        DateTime onUtc,
        CancellationToken cancellationToken)
    {
        List<ProductPrice> candidates = await _context.ProductPrices
            .AsNoTracking()
            .Where(p => p.ProductId == productId && p.CurrencyCode == currencyCode)
            .Where(p => p.ValidFrom == null || p.ValidFrom <= onUtc)
            .Where(p => p.ValidTo == null || p.ValidTo > onUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (candidates.Count == 0)
            return null;

        return candidates
            .OrderByDescending(p => p.ValidFrom.HasValue)
            .ThenByDescending(p => p.ValidFrom)
            .ThenByDescending(p => p.Id)
            .First();
    }
}
