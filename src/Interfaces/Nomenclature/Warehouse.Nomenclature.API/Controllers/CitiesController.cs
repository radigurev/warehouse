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
/// Handles city operations: create, list by state/province, get, update, deactivate, and reactivate.
/// <para>See <see cref="ICityService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cities")]
[Authorize]
public sealed class CitiesController : BaseApiController
{
    private readonly ICityService _cityService;

    /// <summary>
    /// Initializes a new instance with the specified city service.
    /// </summary>
    public CitiesController(ICityService cityService)
    {
        _cityService = cityService;
    }

    /// <summary>
    /// Creates a new city within an active state/province.
    /// </summary>
    [HttpPost]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCityAsync(
        [FromBody] CreateCityRequest request,
        CancellationToken cancellationToken)
    {
        Result<CityDto> result = await _cityService
            .CreateAsync(request, cancellationToken);

        return ToCreatedResult(result, "GetCityById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Gets a list of cities for a given state/province, optionally including inactive ones.
    /// </summary>
    [HttpGet]
    [RequirePermission("nomenclature:read")]
    [ProducesResponseType(typeof(IReadOnlyList<CityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCitiesAsync(
        [FromQuery] int stateProvinceId,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<CityDto>> result = await _cityService
            .ListByStateProvinceAsync(stateProvinceId, includeInactive, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a city by ID.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetCityById")]
    [RequirePermission("nomenclature:read")]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCityByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<CityDto> result = await _cityService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing city by ID.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCityAsync(
        int id,
        [FromBody] UpdateCityRequest request,
        CancellationToken cancellationToken)
    {
        Result<CityDto> result = await _cityService
            .UpdateAsync(id, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deactivates a city.
    /// </summary>
    [HttpPut("{id:int}/deactivate")]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateCityAsync(int id, CancellationToken cancellationToken)
    {
        Result<CityDto> result = await _cityService
            .DeactivateAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Reactivates an inactive city if its parent state/province is active.
    /// </summary>
    [HttpPut("{id:int}/reactivate")]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateCityAsync(int id, CancellationToken cancellationToken)
    {
        Result<CityDto> result = await _cityService
            .ReactivateAsync(id, cancellationToken);

        return ToActionResult(result);
    }
}
