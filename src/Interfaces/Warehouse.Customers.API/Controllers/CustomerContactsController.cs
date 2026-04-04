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
/// Handles customer contact information: addresses, phones, and emails.
/// <para>See <see cref="ICustomerContactService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customers/{customerId:int}")]
[Authorize]
public sealed class CustomerContactsController : BaseCustomersController
{
    private readonly ICustomerContactService _contactService;

    /// <summary>
    /// Initializes a new instance with the specified customer contact service.
    /// </summary>
    public CustomerContactsController(ICustomerContactService contactService)
    {
        _contactService = contactService;
    }

    /// <summary>
    /// Creates a new address for a customer.
    /// </summary>
    [HttpPost("addresses")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(typeof(CustomerAddressDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAddressAsync(
        int customerId,
        [FromBody] CreateAddressRequest request,
        CancellationToken cancellationToken)
    {
        Result<CustomerAddressDto> result = await _contactService
            .CreateAddressAsync(customerId, request, cancellationToken);

        return ToCreatedResult(result, "GetCustomerById", _ => new { id = customerId });
    }

    /// <summary>
    /// Lists all addresses for a customer.
    /// </summary>
    [HttpGet("addresses")]
    [RequirePermission("customers:read")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerAddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAddressesAsync(int customerId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CustomerAddressDto>> result = await _contactService
            .GetAddressesAsync(customerId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing customer address.
    /// </summary>
    [HttpPut("addresses/{addressId:int}")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(typeof(CustomerAddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAddressAsync(
        int customerId,
        int addressId,
        [FromBody] UpdateAddressRequest request,
        CancellationToken cancellationToken)
    {
        Result<CustomerAddressDto> result = await _contactService
            .UpdateAddressAsync(customerId, addressId, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a customer address and promotes the next default if needed.
    /// </summary>
    [HttpDelete("addresses/{addressId:int}")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddressAsync(
        int customerId,
        int addressId,
        CancellationToken cancellationToken)
    {
        Result result = await _contactService
            .DeleteAddressAsync(customerId, addressId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Creates a new phone entry for a customer.
    /// </summary>
    [HttpPost("phones")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(typeof(CustomerPhoneDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePhoneAsync(
        int customerId,
        [FromBody] CreatePhoneRequest request,
        CancellationToken cancellationToken)
    {
        Result<CustomerPhoneDto> result = await _contactService
            .CreatePhoneAsync(customerId, request, cancellationToken);

        return ToCreatedResult(result, "GetCustomerById", _ => new { id = customerId });
    }

    /// <summary>
    /// Lists all phone entries for a customer.
    /// </summary>
    [HttpGet("phones")]
    [RequirePermission("customers:read")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerPhoneDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPhonesAsync(int customerId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CustomerPhoneDto>> result = await _contactService
            .GetPhonesAsync(customerId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing customer phone entry.
    /// </summary>
    [HttpPut("phones/{phoneId:int}")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(typeof(CustomerPhoneDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePhoneAsync(
        int customerId,
        int phoneId,
        [FromBody] UpdatePhoneRequest request,
        CancellationToken cancellationToken)
    {
        Result<CustomerPhoneDto> result = await _contactService
            .UpdatePhoneAsync(customerId, phoneId, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a customer phone entry and promotes the next primary if needed.
    /// </summary>
    [HttpDelete("phones/{phoneId:int}")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePhoneAsync(
        int customerId,
        int phoneId,
        CancellationToken cancellationToken)
    {
        Result result = await _contactService
            .DeletePhoneAsync(customerId, phoneId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Creates a new email entry for a customer.
    /// </summary>
    [HttpPost("emails")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(typeof(CustomerEmailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEmailAsync(
        int customerId,
        [FromBody] CreateEmailRequest request,
        CancellationToken cancellationToken)
    {
        Result<CustomerEmailDto> result = await _contactService
            .CreateEmailAsync(customerId, request, cancellationToken);

        return ToCreatedResult(result, "GetCustomerById", _ => new { id = customerId });
    }

    /// <summary>
    /// Lists all email entries for a customer.
    /// </summary>
    [HttpGet("emails")]
    [RequirePermission("customers:read")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerEmailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmailsAsync(int customerId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CustomerEmailDto>> result = await _contactService
            .GetEmailsAsync(customerId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing customer email entry.
    /// </summary>
    [HttpPut("emails/{emailId:int}")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(typeof(CustomerEmailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateEmailAsync(
        int customerId,
        int emailId,
        [FromBody] UpdateEmailRequest request,
        CancellationToken cancellationToken)
    {
        Result<CustomerEmailDto> result = await _contactService
            .UpdateEmailAsync(customerId, emailId, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes a customer email entry and promotes the next primary if needed.
    /// </summary>
    [HttpDelete("emails/{emailId:int}")]
    [RequirePermission("customers:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmailAsync(
        int customerId,
        int emailId,
        CancellationToken cancellationToken)
    {
        Result result = await _contactService
            .DeleteEmailAsync(customerId, emailId, cancellationToken);

        return ToActionResult(result);
    }
}
