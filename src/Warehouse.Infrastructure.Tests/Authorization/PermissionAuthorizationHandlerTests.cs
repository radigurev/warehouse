using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Warehouse.Infrastructure.Authorization;

namespace Warehouse.Infrastructure.Tests.Authorization;

/// <summary>
/// Tests for <see cref="PermissionAuthorizationHandler"/> covering exact permission matching,
/// wildcard permission matching, and failure scenarios.
/// </summary>
[TestFixture]
[Category("SDD-INFRA-001")]
public sealed class PermissionAuthorizationHandlerTests
{
    private PermissionAuthorizationHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = new PermissionAuthorizationHandler();
    }

    [Test]
    public async Task HandleAsync_UserHasExactPermission_Succeeds()
    {
        // Arrange
        PermissionRequirement requirement = new("customers:read");
        ClaimsPrincipal user = CreateUserWithPermissions("customers:read");
        AuthorizationHandlerContext context = CreateAuthContext(user, requirement);

        // Act
        await ((IAuthorizationHandler)_handler).HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task HandleAsync_UserHasWildcardPermission_Succeeds()
    {
        // Arrange
        PermissionRequirement requirement = new("customers:read");
        ClaimsPrincipal user = CreateUserWithPermissions("customers:all");
        AuthorizationHandlerContext context = CreateAuthContext(user, requirement);

        // Act
        await ((IAuthorizationHandler)_handler).HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task HandleAsync_UserHasDifferentPermission_Fails()
    {
        // Arrange
        PermissionRequirement requirement = new("customers:read");
        ClaimsPrincipal user = CreateUserWithPermissions("inventory:read");
        AuthorizationHandlerContext context = CreateAuthContext(user, requirement);

        // Act
        await ((IAuthorizationHandler)_handler).HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task HandleAsync_UserHasNoPermissionClaims_Fails()
    {
        // Arrange
        PermissionRequirement requirement = new("customers:read");
        ClaimsPrincipal user = new(new ClaimsIdentity([], "TestAuth"));
        AuthorizationHandlerContext context = CreateAuthContext(user, requirement);

        // Act
        await ((IAuthorizationHandler)_handler).HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task HandleAsync_UserHasDifferentResourceWildcard_Fails()
    {
        // Arrange
        PermissionRequirement requirement = new("customers:read");
        ClaimsPrincipal user = CreateUserWithPermissions("inventory:all");
        AuthorizationHandlerContext context = CreateAuthContext(user, requirement);

        // Act
        await ((IAuthorizationHandler)_handler).HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task HandleAsync_UserHasMultiplePermissions_OneMatches_Succeeds()
    {
        // Arrange
        PermissionRequirement requirement = new("customers:write");
        ClaimsPrincipal user = CreateUserWithPermissions("inventory:read", "customers:write", "auth:read");
        AuthorizationHandlerContext context = CreateAuthContext(user, requirement);

        // Act
        await ((IAuthorizationHandler)_handler).HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task HandleAsync_UserHasMultiplePermissions_NoneMatch_Fails()
    {
        // Arrange
        PermissionRequirement requirement = new("customers:delete");
        ClaimsPrincipal user = CreateUserWithPermissions("inventory:read", "auth:write");
        AuthorizationHandlerContext context = CreateAuthContext(user, requirement);

        // Act
        await ((IAuthorizationHandler)_handler).HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.False);
    }

    /// <summary>
    /// Creates a ClaimsPrincipal with the specified permission claim values.
    /// </summary>
    private static ClaimsPrincipal CreateUserWithPermissions(params string[] permissions)
    {
        List<Claim> claims = permissions
            .Select(p => new Claim("permission", p))
            .ToList();

        ClaimsIdentity identity = new(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Creates an AuthorizationHandlerContext for the given user and requirement.
    /// </summary>
    private static AuthorizationHandlerContext CreateAuthContext(
        ClaimsPrincipal user,
        PermissionRequirement requirement)
    {
        IAuthorizationRequirement[] requirements = [requirement];
        return new AuthorizationHandlerContext(requirements, user, null);
    }
}
