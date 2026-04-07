using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Interfaces;

/// <summary>
/// Defines packing operations: create/update/remove parcels, add/remove items.
/// <para>See <see cref="ParcelDto"/>, <see cref="ParcelItemDto"/>.</para>
/// </summary>
public interface IPackingService
{
    /// <summary>Creates a parcel for a sales order.</summary>
    Task<Result<ParcelDto>> CreateParcelAsync(int soId, CreateParcelRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>Gets a parcel by ID with packed items.</summary>
    Task<Result<ParcelDto>> GetParcelByIdAsync(int soId, int parcelId, CancellationToken cancellationToken);

    /// <summary>Lists all parcels for a sales order.</summary>
    Task<Result<IReadOnlyList<ParcelDto>>> ListParcelsAsync(int soId, CancellationToken cancellationToken);

    /// <summary>Updates a parcel (before dispatch only).</summary>
    Task<Result<ParcelDto>> UpdateParcelAsync(int soId, int parcelId, UpdateParcelRequest request, CancellationToken cancellationToken);

    /// <summary>Removes a parcel from a sales order (before dispatch only).</summary>
    Task<Result> RemoveParcelAsync(int soId, int parcelId, CancellationToken cancellationToken);

    /// <summary>Adds an item to a parcel.</summary>
    Task<Result<ParcelItemDto>> AddItemAsync(int soId, int parcelId, AddParcelItemRequest request, CancellationToken cancellationToken);

    /// <summary>Removes an item from a parcel.</summary>
    Task<Result> RemoveItemAsync(int soId, int parcelId, int itemId, CancellationToken cancellationToken);
}
