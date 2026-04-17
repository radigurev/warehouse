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
/// Handles country operations: create, list, get, update, deactivate, and reactivate.
/// <para>See <see cref="ICountryService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/countries")]
[Authorize]
public sealed class CountriesController : BaseApiController
{
    private readonly ICountryService _countryService;

    /// <summary>
    /// Initializes a new instance with the specified country service.
    /// </summary>
    public CountriesController(ICountryService countryService)
    {
        _countryService = countryService;
    }

    /// <summary>
    /// Creates a new country with unique ISO 3166-1 codes.
    /// </summary>
    [HttpPost]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCountryAsync(
        [FromBody] CreateCountryRequest request,
        CancellationToken cancellationToken)
    {
        Result<CountryDto> result = await _countryService
            .CreateAsync(request, cancellationToken);

        return ToCreatedResult(result, "GetCountryById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Gets a list of all countries, optionally including inactive ones.
    /// </summary>
    [HttpGet]
    [RequirePermission("nomenclature:read")]
    [ProducesResponseType(typeof(IReadOnlyList<CountryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCountriesAsync(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<CountryDto>> result = await _countryService
            .ListAsync(includeInactive, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a country by ID with its state/provinces.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetCountryById")]
    [RequirePermission("nomenclature:read")]
    [ProducesResponseType(typeof(CountryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCountryByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<CountryDetailDto> result = await _countryService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing country by ID.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCountryAsync(
        int id,
        [FromBody] UpdateCountryRequest request,
        CancellationToken cancellationToken)
    {
        Result<CountryDto> result = await _countryService
            .UpdateAsync(id, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deactivates a country and cascades deactivation to its state/provinces and cities.
    /// </summary>
    [HttpPut("{id:int}/deactivate")]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateCountryAsync(int id, CancellationToken cancellationToken)
    {
        Result<CountryDto> result = await _countryService
            .DeactivateAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Reactivates an inactive country without reactivating its children.
    /// </summary>
    [HttpPut("{id:int}/reactivate")]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateCountryAsync(int id, CancellationToken cancellationToken)
    {
        Result<CountryDto> result = await _countryService
            .ReactivateAsync(id, cancellationToken);

        return ToActionResult(result);
    }
}
