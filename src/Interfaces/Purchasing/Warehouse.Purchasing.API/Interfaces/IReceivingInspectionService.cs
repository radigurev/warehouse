using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Interfaces;

/// <summary>
/// Defines receiving inspection operations: inspect and resolve quarantine.
/// <para>See <see cref="GoodsReceiptLineDto"/>.</para>
/// </summary>
public interface IReceivingInspectionService
{
    /// <summary>
    /// Inspects a goods receipt line (sets status to Accepted, Rejected, or Quarantined).
    /// </summary>
    Task<Result<GoodsReceiptLineDto>> InspectAsync(int receiptId, int lineId, InspectLineRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a quarantined line to Accepted or Rejected.
    /// </summary>
    Task<Result<GoodsReceiptLineDto>> ResolveQuarantineAsync(int receiptId, int lineId, ResolveQuarantineRequest request, int userId, CancellationToken cancellationToken);
}
