using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Interfaces;

/// <summary>
/// Defines CRUD operations for customer phone entries.
/// <para>See <see cref="CustomerPhoneDto"/>, <see cref="CreatePhoneRequest"/>.</para>
/// </summary>
public interface ICustomerPhoneService
{
    /// <summary>
    /// Creates a new phone entry for a customer.
    /// </summary>
    Task<Result<CustomerPhoneDto>> CreatePhoneAsync(int customerId, CreatePhoneRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all phones for a customer.
    /// </summary>
    Task<Result<IReadOnlyList<CustomerPhoneDto>>> GetPhonesAsync(int customerId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing phone entry.
    /// </summary>
    Task<Result<CustomerPhoneDto>> UpdatePhoneAsync(int customerId, int phoneId, UpdatePhoneRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a phone entry and promotes the next primary if needed.
    /// </summary>
    Task<Result> DeletePhoneAsync(int customerId, int phoneId, CancellationToken cancellationToken);
}
