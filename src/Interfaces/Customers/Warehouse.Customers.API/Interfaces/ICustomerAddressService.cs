using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Interfaces;

/// <summary>
/// Defines CRUD operations for customer addresses.
/// <para>See <see cref="CustomerAddressDto"/>, <see cref="CreateAddressRequest"/>.</para>
/// </summary>
public interface ICustomerAddressService
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
}
