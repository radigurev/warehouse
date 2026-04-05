using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Interfaces;

/// <summary>
/// Defines CRUD operations for customer email entries.
/// <para>See <see cref="CustomerEmailDto"/>, <see cref="CreateEmailRequest"/>.</para>
/// </summary>
public interface ICustomerEmailService
{
    /// <summary>
    /// Creates a new email entry for a customer.
    /// </summary>
    Task<Result<CustomerEmailDto>> CreateEmailAsync(int customerId, CreateEmailRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all emails for a customer.
    /// </summary>
    Task<Result<IReadOnlyList<CustomerEmailDto>>> GetEmailsAsync(int customerId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing email entry.
    /// </summary>
    Task<Result<CustomerEmailDto>> UpdateEmailAsync(int customerId, int emailId, UpdateEmailRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an email entry and promotes the next primary if needed.
    /// </summary>
    Task<Result> DeleteEmailAsync(int customerId, int emailId, CancellationToken cancellationToken);
}
