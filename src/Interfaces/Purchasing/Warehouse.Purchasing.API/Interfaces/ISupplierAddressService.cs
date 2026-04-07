using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Interfaces;

/// <summary>
/// Defines CRUD operations for supplier addresses.
/// <para>See <see cref="SupplierAddressDto"/>.</para>
/// </summary>
public interface ISupplierAddressService
{
    /// <summary>
    /// Creates a new address for a supplier.
    /// </summary>
    Task<Result<SupplierAddressDto>> CreateAsync(int supplierId, CreateSupplierAddressRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all addresses for a supplier.
    /// </summary>
    Task<Result<IReadOnlyList<SupplierAddressDto>>> GetAllAsync(int supplierId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing address.
    /// </summary>
    Task<Result<SupplierAddressDto>> UpdateAsync(int supplierId, int addressId, UpdateSupplierAddressRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an address and promotes the next default if needed.
    /// </summary>
    Task<Result> DeleteAsync(int supplierId, int addressId, CancellationToken cancellationToken);
}
