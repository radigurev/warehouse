using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Interfaces;

/// <summary>
/// Defines customer return (RMA) operations: create, confirm, receive, close, cancel, search.
/// <para>See <see cref="CustomerReturnDetailDto"/>, <see cref="CustomerReturnDto"/>.</para>
/// </summary>
public interface ICustomerReturnService
{
    /// <summary>Creates a new customer return with lines.</summary>
    Task<Result<CustomerReturnDetailDto>> CreateAsync(CreateCustomerReturnRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>Gets a customer return by ID with all lines.</summary>
    Task<Result<CustomerReturnDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Searches customer returns with filters and pagination.</summary>
    Task<Result<PaginatedResponse<CustomerReturnDto>>> SearchAsync(SearchCustomerReturnsRequest request, CancellationToken cancellationToken);

    /// <summary>Confirms a customer return (Draft -> Confirmed).</summary>
    Task<Result<CustomerReturnDetailDto>> ConfirmAsync(int id, int userId, CancellationToken cancellationToken);

    /// <summary>Receives a customer return (Confirmed -> Received) and publishes event.</summary>
    Task<Result<CustomerReturnDetailDto>> ReceiveAsync(int id, int userId, CancellationToken cancellationToken);

    /// <summary>Closes a customer return (Received -> Closed).</summary>
    Task<Result<CustomerReturnDetailDto>> CloseAsync(int id, int userId, CancellationToken cancellationToken);

    /// <summary>Cancels a customer return (Draft or Confirmed -> Cancelled).</summary>
    Task<Result<CustomerReturnDetailDto>> CancelAsync(int id, int userId, CancellationToken cancellationToken);
}
