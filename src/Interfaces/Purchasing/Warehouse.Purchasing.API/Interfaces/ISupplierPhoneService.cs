using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Interfaces;

/// <summary>
/// Defines CRUD operations for supplier phones.
/// <para>See <see cref="SupplierPhoneDto"/>.</para>
/// </summary>
public interface ISupplierPhoneService
{
    /// <summary>
    /// Creates a new phone for a supplier.
    /// </summary>
    Task<Result<SupplierPhoneDto>> CreateAsync(int supplierId, CreateSupplierPhoneRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all phones for a supplier.
    /// </summary>
    Task<Result<IReadOnlyList<SupplierPhoneDto>>> GetAllAsync(int supplierId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing phone.
    /// </summary>
    Task<Result<SupplierPhoneDto>> UpdateAsync(int supplierId, int phoneId, UpdateSupplierPhoneRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a phone and promotes the next primary if needed.
    /// </summary>
    Task<Result> DeleteAsync(int supplierId, int phoneId, CancellationToken cancellationToken);
}
