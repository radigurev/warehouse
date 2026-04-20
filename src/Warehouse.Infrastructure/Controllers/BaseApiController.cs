using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Authorization;

namespace Warehouse.Infrastructure.Controllers;

/// <summary>
/// Base controller providing common helper methods for all API controllers.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Converts a Result to an appropriate ActionResult.
    /// </summary>
    protected IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
            return NoContent();

        return ToProblemResult(result.ErrorCode!, result.ErrorMessage!, result.StatusCode!.Value, result.Extensions);
    }

    /// <summary>
    /// Converts a Result of T to an appropriate ActionResult.
    /// </summary>
    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return ToProblemResult(result.ErrorCode!, result.ErrorMessage!, result.StatusCode!.Value, result.Extensions);
    }

    /// <summary>
    /// Converts a Result of T to a 201 Created response on success.
    /// </summary>
    protected IActionResult ToCreatedResult<T>(Result<T> result, string routeName, Func<T, object> routeValues)
    {
        if (result.IsSuccess)
            return CreatedAtRoute(routeName, routeValues(result.Value!), result.Value);

        return ToProblemResult(result.ErrorCode!, result.ErrorMessage!, result.StatusCode!.Value, result.Extensions);
    }

    /// <summary>
    /// Gets the IP address from the current request.
    /// </summary>
    protected string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Gets the authenticated user ID from the JWT claims.
    /// </summary>
    protected int GetCurrentUserId()
    {
        string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return int.Parse(sub!);
    }

    /// <summary>
    /// Checks whether the current user has the specified permission.
    /// </summary>
    protected async Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken)
    {
        IAuthorizationService authService = HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
        AuthorizationResult result = await authService.AuthorizeAsync(User, null, new PermissionRequirement(permission));
        return result.Succeeded;
    }

    /// <summary>
    /// Builds a ProblemDetails response with optional structured extensions.
    /// </summary>
    protected ObjectResult ToProblemResult(
        string errorCode,
        string errorMessage,
        int statusCode,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        ProblemDetails problem = new()
        {
            Type = $"https://warehouse.local/errors/{errorCode}",
            Title = errorCode,
            Status = statusCode,
            Detail = errorMessage,
            Instance = HttpContext.Request.Path
        };

        if (extensions is not null)
        {
            foreach (KeyValuePair<string, object?> kv in extensions)
                problem.Extensions[kv.Key] = kv.Value;
        }

        return StatusCode(statusCode, problem);
    }
}
