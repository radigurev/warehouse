using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Interfaces;

/// <summary>
/// Defines purchase order lifecycle operations: CRUD, lines, status transitions.
/// <para>See <see cref="PurchaseOrderDetailDto"/>, <see cref="PurchaseOrderDto"/>.</para>
/// </summary>
public interface IPurchaseOrderService
{
    /// <summary>
    /// Creates a new purchase order with lines.
    /// </summary>
    Task<Result<PurchaseOrderDetailDto>> CreateAsync(CreatePurchaseOrderRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a purchase order by ID with lines and receiving progress.
    /// </summary>
    Task<Result<PurchaseOrderDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches purchase orders with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<PurchaseOrderDto>>> SearchAsync(SearchPurchaseOrdersRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates PO header fields (Draft status only).
    /// </summary>
    Task<Result<PurchaseOrderDetailDto>> UpdateHeaderAsync(int id, UpdatePurchaseOrderRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a line to a PO (Draft status only).
    /// </summary>
    Task<Result<PurchaseOrderLineDto>> AddLineAsync(int poId, CreatePurchaseOrderLineRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates a PO line (Draft status only).
    /// </summary>
    Task<Result<PurchaseOrderLineDto>> UpdateLineAsync(int poId, int lineId, UpdatePurchaseOrderLineRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a PO line (Draft status only, cannot remove last line).
    /// </summary>
    Task<Result> RemoveLineAsync(int poId, int lineId, CancellationToken cancellationToken);

    /// <summary>
    /// Confirms a PO (Draft -> Confirmed).
    /// </summary>
    Task<Result<PurchaseOrderDetailDto>> ConfirmAsync(int id, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a PO (Draft or Confirmed, no receipts).
    /// </summary>
    Task<Result<PurchaseOrderDetailDto>> CancelAsync(int id, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Closes a PO (PartiallyReceived or Received).
    /// </summary>
    Task<Result<PurchaseOrderDetailDto>> CloseAsync(int id, int userId, CancellationToken cancellationToken);
}
