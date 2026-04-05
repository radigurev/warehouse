using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Warehouse.Infrastructure.Authorization;

/// <summary>
/// Dynamically creates authorization policies for permission-based requirements.
/// Policies prefixed with "Permission:" are handled by creating a PermissionRequirement.
/// <para>See <see cref="PermissionRequirement"/>, <see cref="PermissionAuthorizationHandler"/>.</para>
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string PermissionPrefix = "Permission:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    /// <summary>
    /// Initializes a new instance with the specified authorization options.
    /// </summary>
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    /// <summary>
    /// Returns the default policy (requires authenticated user).
    /// </summary>
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallback.GetDefaultPolicyAsync();
    }

    /// <summary>
    /// Returns the fallback policy.
    /// </summary>
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallback.GetFallbackPolicyAsync();
    }

    /// <summary>
    /// Returns a policy for the given name. Permission policies are created dynamically.
    /// </summary>
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            string permission = policyName[PermissionPrefix.Length..];

            AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
