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
/// Handles currency operations: create, list, get, update, deactivate, and reactivate.
/// <para>See <see cref="ICurrencyService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/currencies")]
[Authorize]
public sealed class CurrenciesController : BaseApiController
{
    private readonly ICurrencyService _currencyService;

    /// <summary>
    /// Initializes a new instance with the specified currency service.
    /// </summary>
    public CurrenciesController(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    /// <summary>
    /// Creates a new currency with a unique ISO 4217 code.
    /// </summary>
    [HttpPost]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCurrencyAsync(
        [FromBody] CreateCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        Result<CurrencyDto> result = await _currencyService
            .CreateAsync(request, cancellationToken);

        return ToCreatedResult(result, "GetCurrencyById", dto => new { id = dto.Id });
    }

    /// <summary>
    /// Gets a list of all currencies, optionally including inactive ones.
    /// </summary>
    [HttpGet]
    [RequirePermission("nomenclature:read")]
    [ProducesResponseType(typeof(IReadOnlyList<CurrencyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrenciesAsync(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<CurrencyDto>> result = await _currencyService
            .ListAsync(includeInactive, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a currency by ID.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetCurrencyById")]
    [RequirePermission("nomenclature:read")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrencyByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<CurrencyDto> result = await _currencyService
            .GetByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing currency by ID.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCurrencyAsync(
        int id,
        [FromBody] UpdateCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        Result<CurrencyDto> result = await _currencyService
            .UpdateAsync(id, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deactivates a currency.
    /// </summary>
    [HttpPut("{id:int}/deactivate")]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateCurrencyAsync(int id, CancellationToken cancellationToken)
    {
        Result<CurrencyDto> result = await _currencyService
            .DeactivateAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Reactivates an inactive currency.
    /// </summary>
    [HttpPut("{id:int}/reactivate")]
    [RequirePermission("nomenclature:write")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateCurrencyAsync(int id, CancellationToken cancellationToken)
    {
        Result<CurrencyDto> result = await _currencyService
            .ReactivateAsync(id, cancellationToken);

        return ToActionResult(result);
    }
}
