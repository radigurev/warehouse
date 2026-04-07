using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Interfaces;

/// <summary>
/// Defines CRUD operations for supplier emails.
/// <para>See <see cref="SupplierEmailDto"/>.</para>
/// </summary>
public interface ISupplierEmailService
{
    /// <summary>
    /// Creates a new email for a supplier.
    /// </summary>
    Task<Result<SupplierEmailDto>> CreateAsync(int supplierId, CreateSupplierEmailRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all emails for a supplier.
    /// </summary>
    Task<Result<IReadOnlyList<SupplierEmailDto>>> GetAllAsync(int supplierId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing email.
    /// </summary>
    Task<Result<SupplierEmailDto>> UpdateAsync(int supplierId, int emailId, UpdateSupplierEmailRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an email and promotes the next primary if needed.
    /// </summary>
    Task<Result> DeleteAsync(int supplierId, int emailId, CancellationToken cancellationToken);
}
