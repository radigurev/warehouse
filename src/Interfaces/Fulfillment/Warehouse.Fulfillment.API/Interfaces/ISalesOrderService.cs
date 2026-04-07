using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Interfaces;

/// <summary>
/// Defines sales order lifecycle operations: CRUD, lines, status transitions.
/// <para>See <see cref="SalesOrderDetailDto"/>, <see cref="SalesOrderDto"/>.</para>
/// </summary>
public interface ISalesOrderService
{
    /// <summary>Creates a new sales order with lines.</summary>
    Task<Result<SalesOrderDetailDto>> CreateAsync(CreateSalesOrderRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>Gets a sales order by ID with lines and progress.</summary>
    Task<Result<SalesOrderDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Searches sales orders with filters and pagination.</summary>
    Task<Result<PaginatedResponse<SalesOrderDto>>> SearchAsync(SearchSalesOrdersRequest request, CancellationToken cancellationToken);

    /// <summary>Updates SO header fields (Draft status only).</summary>
    Task<Result<SalesOrderDetailDto>> UpdateHeaderAsync(int id, UpdateSalesOrderRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>Adds a line to an SO (Draft status only).</summary>
    Task<Result<SalesOrderLineDto>> AddLineAsync(int soId, CreateSalesOrderLineRequest request, CancellationToken cancellationToken);

    /// <summary>Updates an SO line (Draft status only).</summary>
    Task<Result<SalesOrderLineDto>> UpdateLineAsync(int soId, int lineId, UpdateSalesOrderLineRequest request, CancellationToken cancellationToken);

    /// <summary>Removes an SO line (Draft status only, cannot remove last line).</summary>
    Task<Result> RemoveLineAsync(int soId, int lineId, CancellationToken cancellationToken);

    /// <summary>Confirms an SO (Draft -> Confirmed).</summary>
    Task<Result<SalesOrderDetailDto>> ConfirmAsync(int id, int userId, CancellationToken cancellationToken);

    /// <summary>Cancels an SO (Draft or Confirmed, no pick lists).</summary>
    Task<Result<SalesOrderDetailDto>> CancelAsync(int id, int userId, CancellationToken cancellationToken);

    /// <summary>Completes an SO (Shipped -> Completed).</summary>
    Task<Result<SalesOrderDetailDto>> CompleteAsync(int id, int userId, CancellationToken cancellationToken);
}
