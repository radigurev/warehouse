using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using Warehouse.Infrastructure.Authorization;

namespace Warehouse.Infrastructure.Tests.Authorization;

/// <summary>
/// Tests for <see cref="PermissionAuthorizationHandler"/> covering exact permission matching,
/// wildcard permission matching, and failure scenarios using the real-time permission service.
/// </summary>
[TestFixture]
[Category("SDD-INFRA-001")]
public sealed class PermissionAuthorizationHandlerTests
{
    private Mock<IUserPermissionService> _permissionServiceMock = null!;
    private Mock<ILogger<PermissionAuthorizationHandler>> _loggerMock = null!;
    private PermissionAuthorizationHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _permissionServiceMock = new Mock<IUserPermissionService>();
        _loggerMock = new Mock<ILogger<PermissionAuthorizationHandler>>();
        _handler = new PermissionAuthorizationHandler(_permissionServiceMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task HandleAsync_UserHasExactPermission_Succeeds()
    {
        // Arrange
        PermissionRequirement requirement = new("customers:read");
        ClaimsPrincipal user = CreateUserWithId(1);
        AuthorizationHandlerContext context = CreateAuthContext(user, requirement);
        SetupPermissions(1, "customers:read");

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
        ClaimsPrincipal user = CreateUserWithId(1);
        AuthorizationHandlerContext context = CreateAuthContext(user, requirement);
        SetupPermissions(1, "customers:all");

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
        ClaimsPrincipal user = CreateUserWithId(1);
        AuthorizationHandlerContext context = CreateAuthContext(user, requirement);
        SetupPermissions(1, "inventory:read");

        // Act
        await ((IAuthorizationHandler)_handler).HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task HandleAsync_UserHasNoPermissions_Fails()
    {
        // Arrange
        PermissionRequirement requirement = new("customers:read");
        ClaimsPrincipal user = CreateUserWithId(1);
        AuthorizationHandlerContext context = CreateAuthContext(user, requirement);
        SetupPermissions(1);

        // Act
        await ((IAuthorizationHandler)_handler).HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task HandleAsync_NoUserIdClaim_Fails()
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
        ClaimsPrincipal user = CreateUserWithId(1);
        AuthorizationHandlerContext context = CreateAuthContext(user, requirement);
        SetupPermissions(1, "inventory:all");

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
        ClaimsPrincipal user = CreateUserWithId(1);
        AuthorizationHandlerContext context = CreateAuthContext(user, requirement);
        SetupPermissions(1, "inventory:read", "customers:write", "auth:read");

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
        ClaimsPrincipal user = CreateUserWithId(1);
        AuthorizationHandlerContext context = CreateAuthContext(user, requirement);
        SetupPermissions(1, "inventory:read", "auth:write");

        // Act
        await ((IAuthorizationHandler)_handler).HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.False);
    }

    /// <summary>
    /// Sets up the permission service mock to return the specified permissions for a user.
    /// </summary>
    private void SetupPermissions(int userId, params string[] permissions)
    {
        IReadOnlySet<string> permissionSet = new HashSet<string>(permissions);
        _permissionServiceMock
            .Setup(s => s.GetPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissionSet);
    }

    /// <summary>
    /// Creates a ClaimsPrincipal with a sub (user ID) claim.
    /// </summary>
    private static ClaimsPrincipal CreateUserWithId(int userId)
    {
        List<Claim> claims =
        [
            new Claim("sub", userId.ToString())
        ];

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
