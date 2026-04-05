using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.Inventory.API.Interfaces.Warehouse;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Controllers;

/// <summary>
/// Handles storage location lifecycle operations: create, search, get, update, and delete.
/// <para>See <see cref="IStorageLocationService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/storage-locations")]
[Authorize]
public sealed class StorageLocationsController : BaseApiController
{
    private readonly IStorageLocationService _locationService;

    /// <summary>
    /// Initializes a new instance with the specified storage location service.
    /// </summary>
    public StorageLocationsController(IStorageLocationService locationService)
    {
        _locationService = locationService;
    }

    /// <summary>
    /// Creates a new storage location.
    /// </summary>
    [HttpPost]
    [RequirePermission("storage-locations:create")]
    [ProducesResponseType(typeof(StorageLocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateLocationAsync(
        [FromBody] CreateStorageLocationRequest request,
        CancellationToken cancellationToken)
    {
        Result<StorageLocationDto> result = await _locationService
            .CreateAsync(request, cancellationToken);

        return ToCreatedResult(result, "GetLocationById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Searches storage locations with filters and pagination.
    /// </summary>
    [HttpGet]
    [RequirePermission("storage-locations:read")]
    [ProducesResponseType(typeof(PaginatedResponse<StorageLocationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchLocationsAsync(
        [FromQuery] SearchStorageLocationsRequest request,
        CancellationToken cancellationToken)
    {
        Result<PaginatedResponse<StorageLocationDto>> result = await _locationService
            .SearchAsync(request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a storage location by ID.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetLocationById")]
    [RequirePermission("storage-locations:read")]
    [ProducesResponseType(typeof(StorageLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLocationByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<StorageLocationDto> result = await _locationService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing storage location.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("storage-locations:update")]
    [ProducesResponseType(typeof(StorageLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLocationAsync(
        int id,
        [FromBody] UpdateStorageLocationRequest request,
        CancellationToken cancellationToken)
    {
        Result<StorageLocationDto> result = await _locationService
            .UpdateAsync(id, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a storage location.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("storage-locations:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLocationAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _locationService.DeleteAsync(id, cancellationToken);
        return ToActionResult(result);
    }
}
