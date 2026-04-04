using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Interfaces;

/// <summary>
/// Defines contact information operations for addresses, phones, and emails.
/// <para>See <see cref="CustomerAddressDto"/>, <see cref="CustomerPhoneDto"/>, <see cref="CustomerEmailDto"/>.</para>
/// </summary>
public interface ICustomerContactService
{
    /// <summary>
    /// Creates a new address for a customer.
    /// </summary>
    Task<Result<CustomerAddressDto>> CreateAddressAsync(int customerId, CreateAddressRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all addresses for a customer.
    /// </summary>
    Task<Result<IReadOnlyList<CustomerAddressDto>>> GetAddressesAsync(int customerId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing address.
    /// </summary>
    Task<Result<CustomerAddressDto>> UpdateAddressAsync(int customerId, int addressId, UpdateAddressRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an address and promotes the next default if needed.
    /// </summary>
    Task<Result> DeleteAddressAsync(int customerId, int addressId, CancellationToken cancellationToken);

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
