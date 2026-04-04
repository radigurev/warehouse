using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Customers.API.Authorization;
using Warehouse.Customers.API.Interfaces;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Controllers;

/// <summary>
/// Handles customer account operations: create, list, update, deactivate, and merge.
/// <para>See <see cref="ICustomerAccountService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customers/{customerId:int}/accounts")]
[Authorize]
public sealed class CustomerAccountsController : BaseCustomersController
{
    private readonly ICustomerAccountService _accountService;

    /// <summary>
    /// Initializes a new instance with the specified customer account service.
    /// </summary>
    public CustomerAccountsController(ICustomerAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Creates a new account for a customer with the specified currency.
    /// </summary>
    [HttpPost]
    [RequirePermission("customers:update")]
    [ProducesResponseType(typeof(CustomerAccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAccountAsync(
        int customerId,
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        Result<CustomerAccountDto> result = await _accountService
            .CreateAsync(customerId, request, cancellationToken);

        return ToCreatedResult(result, "GetCustomerById", _ => new { id = customerId });
    }

    /// <summary>
    /// Lists all non-deleted accounts for a customer.
    /// </summary>
    [HttpGet]
    [RequirePermission("customers:read")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountsAsync(int customerId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CustomerAccountDto>> result = await _accountService
            .GetByCustomerIdAsync(customerId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an account's description and primary flag.
    /// </summary>
    [HttpPut("{accountId:int}")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(typeof(CustomerAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAccountAsync(
        int customerId,
        int accountId,
        [FromBody] UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        Result<CustomerAccountDto> result = await _accountService
            .UpdateAsync(customerId, accountId, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Soft-deletes an account after verifying zero balance and not the last active account.
    /// </summary>
    [HttpDelete("{accountId:int}")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateAccountAsync(
        int customerId,
        int accountId,
        CancellationToken cancellationToken)
    {
        Result result = await _accountService
            .DeactivateAsync(customerId, accountId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Merges two same-currency accounts belonging to the same customer within a transaction.
    /// </summary>
    [HttpPost("merge")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(typeof(CustomerAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MergeAccountsAsync(
        int customerId,
        [FromBody] MergeAccountsRequest request,
        CancellationToken cancellationToken)
    {
        Result<CustomerAccountDto> result = await _accountService
            .MergeAsync(customerId, request, cancellationToken);

        return ToActionResult(result);
    }
}
