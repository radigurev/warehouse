using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Controllers;

/// <summary>
/// Handles packing operations: parcel CRUD and item management.
/// <para>See <see cref="IPackingService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sales-orders/{soId:int}/parcels")]
[Authorize]
public sealed class PackingController : BaseApiController
{
    private readonly IPackingService _packingService;

    /// <summary>Initializes a new instance with the specified packing service.</summary>
    public PackingController(IPackingService packingService) { _packingService = packingService; }

    /// <summary>Creates a parcel for a sales order.</summary>
    [HttpPost]
    [RequirePermission("packing:create")]
    [ProducesResponseType(typeof(ParcelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateParcelAsync(int soId, [FromBody] CreateParcelRequest request, CancellationToken cancellationToken)
    { int userId = GetCurrentUserId(); Result<ParcelDto> result = await _packingService.CreateParcelAsync(soId, request, userId, cancellationToken); return ToCreatedResult(result, "GetParcelById", dto => new { soId, parcelId = dto.Id }); }

    /// <summary>Lists all parcels for a sales order.</summary>
    [HttpGet]
    [RequirePermission("packing:read")]
    [ProducesResponseType(typeof(IReadOnlyList<ParcelDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListParcelsAsync(int soId, CancellationToken cancellationToken)
    { Result<IReadOnlyList<ParcelDto>> result = await _packingService.ListParcelsAsync(soId, cancellationToken); return ToActionResult(result); }

    /// <summary>Gets a parcel by ID with packed items.</summary>
    [HttpGet("{parcelId:int}", Name = "GetParcelById")]
    [RequirePermission("packing:read")]
    [ProducesResponseType(typeof(ParcelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParcelByIdAsync(int soId, int parcelId, CancellationToken cancellationToken)
    { Result<ParcelDto> result = await _packingService.GetParcelByIdAsync(soId, parcelId, cancellationToken); return ToActionResult(result); }

    /// <summary>Updates a parcel (before dispatch only).</summary>
    [HttpPut("{parcelId:int}")]
    [RequirePermission("packing:update")]
    [ProducesResponseType(typeof(ParcelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateParcelAsync(int soId, int parcelId, [FromBody] UpdateParcelRequest request, CancellationToken cancellationToken)
    { Result<ParcelDto> result = await _packingService.UpdateParcelAsync(soId, parcelId, request, cancellationToken); return ToActionResult(result); }

    /// <summary>Removes a parcel from a sales order.</summary>
    [HttpDelete("{parcelId:int}")]
    [RequirePermission("packing:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveParcelAsync(int soId, int parcelId, CancellationToken cancellationToken)
    { Result result = await _packingService.RemoveParcelAsync(soId, parcelId, cancellationToken); return ToActionResult(result); }

    /// <summary>Adds an item to a parcel.</summary>
    [HttpPost("{parcelId:int}/items")]
    [RequirePermission("packing:update")]
    [ProducesResponseType(typeof(ParcelItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddItemAsync(int soId, int parcelId, [FromBody] AddParcelItemRequest request, CancellationToken cancellationToken)
    { Result<ParcelItemDto> result = await _packingService.AddItemAsync(soId, parcelId, request, cancellationToken); return ToCreatedResult(result, "GetParcelById", _ => new { soId, parcelId }); }

    /// <summary>Removes an item from a parcel.</summary>
    [HttpDelete("{parcelId:int}/items/{itemId:int}")]
    [RequirePermission("packing:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItemAsync(int soId, int parcelId, int itemId, CancellationToken cancellationToken)
    { Result result = await _packingService.RemoveItemAsync(soId, parcelId, itemId, cancellationToken); return ToActionResult(result); }
}
