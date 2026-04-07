using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;
using Warehouse.Infrastructure.Controllers;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Controllers;

/// <summary>
/// Handles supplier contact information: addresses, phones, and emails.
/// <para>See <see cref="ISupplierAddressService"/>, <see cref="ISupplierPhoneService"/>, <see cref="ISupplierEmailService"/>.</para>
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/suppliers/{supplierId:int}")]
[Authorize]
public sealed class SupplierContactsController : BaseApiController
{
    private readonly ISupplierAddressService _addressService;
    private readonly ISupplierPhoneService _phoneService;
    private readonly ISupplierEmailService _emailService;

    /// <summary>
    /// Initializes a new instance with the specified contact services.
    /// </summary>
    public SupplierContactsController(ISupplierAddressService addressService, ISupplierPhoneService phoneService, ISupplierEmailService emailService)
    { _addressService = addressService; _phoneService = phoneService; _emailService = emailService; }

    /// <summary>Creates a new address for a supplier.</summary>
    [HttpPost("addresses")]
    [RequirePermission("suppliers:update")]
    [ProducesResponseType(typeof(SupplierAddressDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAddressAsync(int supplierId, [FromBody] CreateSupplierAddressRequest request, CancellationToken cancellationToken)
    { Result<SupplierAddressDto> result = await _addressService.CreateAsync(supplierId, request, cancellationToken); return ToCreatedResult(result, "GetSupplierById", _ => new { id = supplierId }); }

    /// <summary>Lists all addresses for a supplier.</summary>
    [HttpGet("addresses")]
    [RequirePermission("suppliers:read")]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierAddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAddressesAsync(int supplierId, CancellationToken cancellationToken)
    { Result<IReadOnlyList<SupplierAddressDto>> result = await _addressService.GetAllAsync(supplierId, cancellationToken); return ToActionResult(result); }

    /// <summary>Updates an existing supplier address.</summary>
    [HttpPut("addresses/{addressId:int}")]
    [RequirePermission("suppliers:update")]
    [ProducesResponseType(typeof(SupplierAddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAddressAsync(int supplierId, int addressId, [FromBody] UpdateSupplierAddressRequest request, CancellationToken cancellationToken)
    { Result<SupplierAddressDto> result = await _addressService.UpdateAsync(supplierId, addressId, request, cancellationToken); return ToActionResult(result); }

    /// <summary>Deletes a supplier address and promotes the next default if needed.</summary>
    [HttpDelete("addresses/{addressId:int}")]
    [RequirePermission("suppliers:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddressAsync(int supplierId, int addressId, CancellationToken cancellationToken)
    { Result result = await _addressService.DeleteAsync(supplierId, addressId, cancellationToken); return ToActionResult(result); }

    /// <summary>Creates a new phone entry for a supplier.</summary>
    [HttpPost("phones")]
    [RequirePermission("suppliers:update")]
    [ProducesResponseType(typeof(SupplierPhoneDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePhoneAsync(int supplierId, [FromBody] CreateSupplierPhoneRequest request, CancellationToken cancellationToken)
    { Result<SupplierPhoneDto> result = await _phoneService.CreateAsync(supplierId, request, cancellationToken); return ToCreatedResult(result, "GetSupplierById", _ => new { id = supplierId }); }

    /// <summary>Lists all phone entries for a supplier.</summary>
    [HttpGet("phones")]
    [RequirePermission("suppliers:read")]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierPhoneDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPhonesAsync(int supplierId, CancellationToken cancellationToken)
    { Result<IReadOnlyList<SupplierPhoneDto>> result = await _phoneService.GetAllAsync(supplierId, cancellationToken); return ToActionResult(result); }

    /// <summary>Updates an existing supplier phone entry.</summary>
    [HttpPut("phones/{phoneId:int}")]
    [RequirePermission("suppliers:update")]
    [ProducesResponseType(typeof(SupplierPhoneDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePhoneAsync(int supplierId, int phoneId, [FromBody] UpdateSupplierPhoneRequest request, CancellationToken cancellationToken)
    { Result<SupplierPhoneDto> result = await _phoneService.UpdateAsync(supplierId, phoneId, request, cancellationToken); return ToActionResult(result); }

    /// <summary>Deletes a supplier phone entry and promotes the next primary if needed.</summary>
    [HttpDelete("phones/{phoneId:int}")]
    [RequirePermission("suppliers:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePhoneAsync(int supplierId, int phoneId, CancellationToken cancellationToken)
    { Result result = await _phoneService.DeleteAsync(supplierId, phoneId, cancellationToken); return ToActionResult(result); }

    /// <summary>Creates a new email entry for a supplier.</summary>
    [HttpPost("emails")]
    [RequirePermission("suppliers:update")]
    [ProducesResponseType(typeof(SupplierEmailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEmailAsync(int supplierId, [FromBody] CreateSupplierEmailRequest request, CancellationToken cancellationToken)
    { Result<SupplierEmailDto> result = await _emailService.CreateAsync(supplierId, request, cancellationToken); return ToCreatedResult(result, "GetSupplierById", _ => new { id = supplierId }); }

    /// <summary>Lists all email entries for a supplier.</summary>
    [HttpGet("emails")]
    [RequirePermission("suppliers:read")]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierEmailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmailsAsync(int supplierId, CancellationToken cancellationToken)
    { Result<IReadOnlyList<SupplierEmailDto>> result = await _emailService.GetAllAsync(supplierId, cancellationToken); return ToActionResult(result); }

    /// <summary>Updates an existing supplier email entry.</summary>
    [HttpPut("emails/{emailId:int}")]
    [RequirePermission("suppliers:update")]
    [ProducesResponseType(typeof(SupplierEmailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateEmailAsync(int supplierId, int emailId, [FromBody] UpdateSupplierEmailRequest request, CancellationToken cancellationToken)
    { Result<SupplierEmailDto> result = await _emailService.UpdateAsync(supplierId, emailId, request, cancellationToken); return ToActionResult(result); }

    /// <summary>Deletes a supplier email entry and promotes the next primary if needed.</summary>
    [HttpDelete("emails/{emailId:int}")]
    [RequirePermission("suppliers:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmailAsync(int supplierId, int emailId, CancellationToken cancellationToken)
    { Result result = await _emailService.DeleteAsync(supplierId, emailId, cancellationToken); return ToActionResult(result); }
}
