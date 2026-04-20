using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Interfaces;

/// <summary>
/// Defines CRUD and diagnostic resolution operations for the Fulfillment Product Price Catalog.
/// <para>Conforms to CHG-FEAT-007 §2.2 Catalog CRUD and §2.5 Diagnostic Resolver Endpoint.</para>
/// </summary>
public interface IProductPriceService
{
    /// <summary>Creates a new product price entry.</summary>
    Task<Result<ProductPriceDto>> CreateAsync(CreateProductPriceRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>Gets a product price by ID.</summary>
    Task<Result<ProductPriceDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Searches product prices with filters and pagination.</summary>
    Task<Result<PaginatedResponse<ProductPriceDto>>> SearchAsync(SearchProductPricesRequest request, CancellationToken cancellationToken);

    /// <summary>Updates an existing product price (UnitPrice and validity window only).</summary>
    Task<Result<ProductPriceDto>> UpdateAsync(int id, UpdateProductPriceRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>Hard-deletes a product price; historical SO line snapshots are preserved.</summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>Diagnostic resolver: returns the effective price for the given tuple or a not-found failure.</summary>
    Task<Result<ProductPriceDto>> ResolveAsync(int productId, string currencyCode, DateTime? onDateUtc, CancellationToken cancellationToken);
}
