using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Nomenclature.API.Interfaces;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.ServiceModel.DTOs.Nomenclature;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Controllers;

/// <summary>
/// Handles state/province operations: create, list by country, get, update, deactivate, and reactivate.
/// <para>See <see cref="IStateProvinceService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/state-provinces")]
[Authorize]
public sealed class StateProvincesController : BaseApiController
{
    private readonly IStateProvinceService _stateProvinceService;

    /// <summary>
    /// Initializes a new instance with the specified state/province service.
    /// </summary>
    public StateProvincesController(IStateProvinceService stateProvinceService)
    {
        _stateProvinceService = stateProvinceService;
    }

    /// <summary>
    /// Creates a new state/province within an active country.
    /// </summary>
    [HttpPost]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(StateProvinceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStateProvinceAsync(
        [FromBody] CreateStateProvinceRequest request,
        CancellationToken cancellationToken)
    {
        Result<StateProvinceDto> result = await _stateProvinceService
            .CreateAsync(request, cancellationToken);

        return ToCreatedResult(result, "GetStateProvinceById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Gets a list of state/provinces for a given country, optionally including inactive ones.
    /// </summary>
    [HttpGet]
    [RequirePermission("nomenclature:read")]
    [ProducesResponseType(typeof(IReadOnlyList<StateProvinceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStateProvincesAsync(
        [FromQuery] int countryId,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<StateProvinceDto>> result = await _stateProvinceService
            .ListByCountryAsync(countryId, includeInactive, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a state/province by ID.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetStateProvinceById")]
    [RequirePermission("nomenclature:read")]
    [ProducesResponseType(typeof(StateProvinceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStateProvinceByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<StateProvinceDto> result = await _stateProvinceService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing state/province by ID.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(StateProvinceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStateProvinceAsync(
        int id,
        [FromBody] UpdateStateProvinceRequest request,
        CancellationToken cancellationToken)
    {
        Result<StateProvinceDto> result = await _stateProvinceService
            .UpdateAsync(id, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deactivates a state/province and cascades deactivation to its cities.
    /// </summary>
    [HttpPut("{id:int}/deactivate")]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(StateProvinceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateStateProvinceAsync(int id, CancellationToken cancellationToken)
    {
        Result<StateProvinceDto> result = await _stateProvinceService
            .DeactivateAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Reactivates an inactive state/province if its parent country is active.
    /// </summary>
    [HttpPut("{id:int}/reactivate")]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(StateProvinceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateStateProvinceAsync(int id, CancellationToken cancellationToken)
    {
        Result<StateProvinceDto> result = await _stateProvinceService
            .ReactivateAsync(id, cancellationToken);

        return ToActionResult(result);
    }
}
