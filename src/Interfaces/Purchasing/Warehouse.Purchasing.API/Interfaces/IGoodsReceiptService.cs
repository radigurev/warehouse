using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Interfaces;

/// <summary>
/// Defines goods receipt operations: create, complete, search.
/// <para>See <see cref="GoodsReceiptDetailDto"/>, <see cref="GoodsReceiptDto"/>.</para>
/// </summary>
public interface IGoodsReceiptService
{
    /// <summary>
    /// Creates a new goods receipt with lines against a purchase order.
    /// </summary>
    Task<Result<GoodsReceiptDetailDto>> CreateAsync(CreateGoodsReceiptRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a goods receipt by ID with all lines.
    /// </summary>
    Task<Result<GoodsReceiptDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches goods receipts with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<GoodsReceiptDto>>> SearchAsync(SearchGoodsReceiptsRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Completes a goods receipt, publishes event, and updates PO status.
    /// </summary>
    Task<Result<GoodsReceiptDetailDto>> CompleteAsync(int id, int userId, CancellationToken cancellationToken);
}
