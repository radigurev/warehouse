using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.Inventory.API.Interfaces.Stock;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Controllers;

/// <summary>
/// Handles batch lifecycle operations: create, search, get, update, and deactivate.
/// <para>See <see cref="IBatchService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/batches")]
[Authorize]
public sealed class BatchesController : BaseApiController
{
    private readonly IBatchService _batchService;

    /// <summary>
    /// Initializes a new instance with the specified batch service.
    /// </summary>
    public BatchesController(IBatchService batchService)
    {
        _batchService = batchService;
    }

    /// <summary>
    /// Creates a new batch.
    /// </summary>
    [HttpPost]
    [RequirePermission("batches:create")]
    [ProducesResponseType(typeof(BatchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBatchAsync(
        [FromBody] CreateBatchRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<BatchDto> result = await _batchService
            .CreateAsync(request, userId, cancellationToken);

        return ToCreatedResult(result, "GetBatchById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Searches batches with filters and pagination.
    /// </summary>
    [HttpGet]
    [RequirePermission("batches:read")]
    [ProducesResponseType(typeof(PaginatedResponse<BatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchBatchesAsync(
        [FromQuery] SearchBatchesRequest request,
        CancellationToken cancellationToken)
    {
        Result<PaginatedResponse<BatchDto>> result = await _batchService
            .SearchAsync(request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a batch by ID.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetBatchById")]
    [RequirePermission("batches:read")]
    [ProducesResponseType(typeof(BatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<BatchDto> result = await _batchService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing batch.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("batches:update")]
    [ProducesResponseType(typeof(BatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBatchAsync(
        int id,
        [FromBody] UpdateBatchRequest request,
        CancellationToken cancellationToken)
    {
        int userId = GetCurrentUserId();

        Result<BatchDto> result = await _batchService
            .UpdateAsync(id, request, userId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deactivates a batch.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("batches:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateBatchAsync(int id, CancellationToken cancellationToken)
    {
        Result result = await _batchService.DeactivateAsync(id, cancellationToken);
        return ToActionResult(result);
    }
}
