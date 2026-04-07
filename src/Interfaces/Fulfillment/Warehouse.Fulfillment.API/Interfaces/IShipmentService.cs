using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Interfaces;

/// <summary>
/// Defines shipment operations: dispatch, status updates, tracking, search.
/// <para>See <see cref="ShipmentDetailDto"/>, <see cref="ShipmentDto"/>.</para>
/// </summary>
public interface IShipmentService
{
    /// <summary>Creates a shipment (dispatches a packed sales order).</summary>
    Task<Result<ShipmentDetailDto>> CreateAsync(CreateShipmentRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>Gets a shipment by ID with lines, parcels, and tracking history.</summary>
    Task<Result<ShipmentDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Searches shipments with filters and pagination.</summary>
    Task<Result<PaginatedResponse<ShipmentDto>>> SearchAsync(SearchShipmentsRequest request, CancellationToken cancellationToken);

    /// <summary>Updates shipment status with tracking information.</summary>
    Task<Result<ShipmentDetailDto>> UpdateStatusAsync(int id, UpdateShipmentStatusRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>Gets the full tracking history for a shipment.</summary>
    Task<Result<IReadOnlyList<ShipmentTrackingDto>>> GetTrackingHistoryAsync(int id, CancellationToken cancellationToken);
}
