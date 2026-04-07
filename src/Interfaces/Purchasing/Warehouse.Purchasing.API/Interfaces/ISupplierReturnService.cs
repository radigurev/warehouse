using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Interfaces;

/// <summary>
/// Defines supplier return operations: create, confirm, cancel, search.
/// <para>See <see cref="SupplierReturnDetailDto"/>, <see cref="SupplierReturnDto"/>.</para>
/// </summary>
public interface ISupplierReturnService
{
    /// <summary>
    /// Creates a new supplier return with lines.
    /// </summary>
    Task<Result<SupplierReturnDetailDto>> CreateAsync(CreateSupplierReturnRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a supplier return by ID with all lines.
    /// </summary>
    Task<Result<SupplierReturnDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches supplier returns with filters and pagination.
    /// </summary>
    Task<Result<PaginatedResponse<SupplierReturnDto>>> SearchAsync(SearchSupplierReturnsRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Confirms a supplier return (publishes event and generates stock movements).
    /// </summary>
    Task<Result<SupplierReturnDetailDto>> ConfirmAsync(int id, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a draft supplier return.
    /// </summary>
    Task<Result<SupplierReturnDetailDto>> CancelAsync(int id, int userId, CancellationToken cancellationToken);
}
