using Warehouse.Common.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Interfaces;

/// <summary>
/// Defines pick list operations: generate, confirm pick, cancel, search.
/// <para>See <see cref="PickListDetailDto"/>, <see cref="PickListDto"/>.</para>
/// </summary>
public interface IPickListService
{
    /// <summary>Generates a pick list from a confirmed sales order.</summary>
    Task<Result<PickListDetailDto>> GenerateAsync(GeneratePickListRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>Gets a pick list by ID with all lines.</summary>
    Task<Result<PickListDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Searches pick lists with filters and pagination.</summary>
    Task<Result<PaginatedResponse<PickListDto>>> SearchAsync(SearchPickListsRequest request, CancellationToken cancellationToken);

    /// <summary>Confirms a pick list line as picked.</summary>
    Task<Result<PickListLineDto>> ConfirmPickAsync(int pickListId, int lineId, ConfirmPickRequest request, int userId, CancellationToken cancellationToken);

    /// <summary>Cancels a pending pick list and releases reservations.</summary>
    Task<Result<PickListDetailDto>> CancelAsync(int id, int userId, CancellationToken cancellationToken);
}
