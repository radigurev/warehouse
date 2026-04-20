using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Warehouse.Auth.API.Tests.Fixtures;
using Warehouse.Auth.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses;
using Warehouse.ServiceModel.Responses.Auth;

namespace Warehouse.Auth.API.Tests.Integration;

/// <summary>
/// Integration tests for real-time permission validation via Redis (CHG-ENH-004).
/// Covers: resolved permission endpoint, JWT identity-only claims, permission revocation,
/// and cache invalidation on role assignment and role permission changes.
/// </summary>
[TestFixture]
[Category("CHG-ENH-004")]
[Category("Integration")]
public sealed class PermissionValidationTests : AuthApiTestBase
{
    [Test]
    public async Task GetUserPermissions_ReturnsResolvedSet()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        int adminUserId = await ReadDbContextAsync(async ctx =>
        {
            User? admin = await ctx.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            return admin!.Id;
        });

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/users/{adminUserId}/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        UserPermissionsResponse? body = await response.Content.ReadFromJsonAsync<UserPermissionsResponse>();
        body.Should().NotBeNull();
        body!.UserId.Should().Be(adminUserId);
        body.Permissions.Should().NotBeEmpty();
        body.Permissions.Should().Contain("users:read");
        body.Permissions.Should().Contain("customers:read");
        body.Permissions.Should().Contain("products:read");
    }

    [Test]
    public async Task GetUserPermissions_NonExistentUser_Returns404()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/users/99999/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Login_TokenContainsOnlyIdentityClaims()
    {
        // Arrange
        HttpClient client = CreateClient();
        LoginRequest request = new() { Username = AdminUsername, Password = AdminPassword };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        LoginResponse? loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();

        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(loginResponse!.AccessToken);
        HashSet<string> allowedClaimTypes = new(["sub", "username", "jti", "iat", "exp", "iss", "aud", "nbf"]);

        List<string> unexpectedClaims = jwt.Claims
            .Where(c => !allowedClaimTypes.Contains(c.Type))
            .Select(c => c.Type)
            .ToList();

        unexpectedClaims.Should().BeEmpty(
            "JWT should contain only identity claims — permissions are resolved at request time via Redis/Auth.API");

        jwt.Claims.Should().Contain(c => c.Type == "sub", "JWT must contain sub claim");
        jwt.Claims.Should().Contain(c => c.Type == "username", "JWT must contain username claim");
        jwt.Claims.Should().Contain(c => c.Type == "jti", "JWT must contain jti claim");
    }

    [Test]
    public async Task PermissionRevoked_NextRequestDenied()
    {
        // Arrange — create user, role with users:read, assign role, login as user
        HttpClient adminClient = await CreateAuthenticatedClientAsync();

        CreateRoleRequest roleRequest = new() { Name = "LimitedViewer", Description = "Can only view users" };
        HttpResponseMessage createRoleResponse = await adminClient.PostAsJsonAsync("/api/v1/roles", roleRequest);
        createRoleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        RoleDetailDto? createdRole = await createRoleResponse.Content.ReadFromJsonAsync<RoleDetailDto>();
        int roleId = createdRole!.Id;

        int usersReadPermissionId = await ReadDbContextAsync(async ctx =>
        {
            Permission? perm = await ctx.Permissions
                .FirstOrDefaultAsync(p => p.Resource == "users" && p.Action == "read");
            return perm!.Id;
        });

        AssignPermissionsRequest assignPermsRequest = new() { PermissionIds = [usersReadPermissionId] };
        HttpResponseMessage assignPermsResponse = await adminClient
            .PostAsJsonAsync($"/api/v1/roles/{roleId}/permissions", assignPermsRequest);
        assignPermsResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage createUserResponse = await CreateUserViaApiAsync(
            adminClient, "limiteduser", "limited@warehouse.local", "LimitedPass1!");
        CreateUserResponse? createdUser = await createUserResponse.Content.ReadFromJsonAsync<CreateUserResponse>();
        int userId = createdUser!.Id;

        AssignRolesRequest assignRolesRequest = new() { RoleIds = [roleId] };
        HttpResponseMessage assignRolesResponse = await adminClient
            .PostAsJsonAsync($"/api/v1/users/{userId}/roles", assignRolesRequest);
        assignRolesResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpClient userClient = await CreateAuthenticatedClientAsync("limiteduser", "LimitedPass1!");

        // Act 1 — user can access users endpoint
        HttpResponseMessage allowedResponse = await userClient.GetAsync("/api/v1/users");
        allowedResponse.StatusCode.Should().Be(HttpStatusCode.OK, "User with users:read should access /users");

        // Revoke the role via admin
        HttpResponseMessage removeRoleResponse = await adminClient
            .DeleteAsync($"/api/v1/users/{userId}/roles/{roleId}");
        removeRoleResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act 2 — next request should be denied (same JWT, but permissions resolved from DB)
        HttpResponseMessage deniedResponse = await userClient.GetAsync("/api/v1/users");

        // Assert
        deniedResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "After role revocation, user should be denied access on next request");
    }

    [Test]
    public async Task RoleAssignmentChange_PermissionsEndpointReflectsUpdate()
    {
        // Arrange — create user with no roles, verify empty permissions, assign role, verify permissions
        HttpClient adminClient = await CreateAuthenticatedClientAsync();

        HttpResponseMessage createUserResponse = await CreateUserViaApiAsync(
            adminClient, "noroleuser", "norole@warehouse.local", "NoRole1!");
        CreateUserResponse? createdUser = await createUserResponse.Content.ReadFromJsonAsync<CreateUserResponse>();
        int userId = createdUser!.Id;

        // Act 1 — user has no roles, permissions should be empty
        HttpResponseMessage emptyResponse = await adminClient
            .GetAsync($"/api/v1/users/{userId}/permissions");
        emptyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        UserPermissionsResponse? emptyBody = await emptyResponse.Content
            .ReadFromJsonAsync<UserPermissionsResponse>();
        emptyBody!.Permissions.Should().BeEmpty("Newly created user has no roles — no permissions");

        // Assign Admin role
        int adminRoleId = await ReadDbContextAsync(async ctx =>
        {
            Role? role = await ctx.Roles.FirstOrDefaultAsync(r => r.Name == "Admin" && r.IsSystem);
            return role!.Id;
        });
        AssignRolesRequest assignRequest = new() { RoleIds = [adminRoleId] };
        HttpResponseMessage assignResponse = await adminClient
            .PostAsJsonAsync($"/api/v1/users/{userId}/roles", assignRequest);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act 2 — permissions should now include admin permissions
        HttpResponseMessage fullResponse = await adminClient
            .GetAsync($"/api/v1/users/{userId}/permissions");
        fullResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        UserPermissionsResponse? fullBody = await fullResponse.Content
            .ReadFromJsonAsync<UserPermissionsResponse>();

        // Assert
        fullBody!.Permissions.Should().NotBeEmpty("User with Admin role should have permissions");
        fullBody.Permissions.Should().Contain("users:read");
    }

    [Test]
    public async Task RolePermissionChange_PermissionsEndpointReflectsUpdate()
    {
        // Arrange — create a role, assign to user, verify permissions, add more permissions to role
        HttpClient adminClient = await CreateAuthenticatedClientAsync();

        CreateRoleRequest roleRequest = new() { Name = "GrowingRole", Description = "Role that gains permissions" };
        HttpResponseMessage createRoleResponse = await adminClient.PostAsJsonAsync("/api/v1/roles", roleRequest);
        createRoleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        RoleDetailDto? createdRole = await createRoleResponse.Content.ReadFromJsonAsync<RoleDetailDto>();
        int roleId = createdRole!.Id;

        int auditReadPermissionId = await ReadDbContextAsync(async ctx =>
        {
            Permission? perm = await ctx.Permissions
                .FirstOrDefaultAsync(p => p.Resource == "audit" && p.Action == "read");
            return perm!.Id;
        });
        AssignPermissionsRequest firstPermRequest = new() { PermissionIds = [auditReadPermissionId] };
        HttpResponseMessage assignPermResponse = await adminClient
            .PostAsJsonAsync($"/api/v1/roles/{roleId}/permissions", firstPermRequest);
        assignPermResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage createUserResponse = await CreateUserViaApiAsync(
            adminClient, "growinguser", "growing@warehouse.local", "Growing1!");
        CreateUserResponse? createdUser = await createUserResponse.Content.ReadFromJsonAsync<CreateUserResponse>();
        int userId = createdUser!.Id;

        AssignRolesRequest assignRolesRequest = new() { RoleIds = [roleId] };
        HttpResponseMessage assignRolesResponse = await adminClient
            .PostAsJsonAsync($"/api/v1/users/{userId}/roles", assignRolesRequest);
        assignRolesResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act 1 — user has only audit:read
        HttpResponseMessage beforeResponse = await adminClient
            .GetAsync($"/api/v1/users/{userId}/permissions");
        UserPermissionsResponse? beforeBody = await beforeResponse.Content
            .ReadFromJsonAsync<UserPermissionsResponse>();
        beforeBody!.Permissions.Should().HaveCount(1);
        beforeBody.Permissions.Should().Contain("audit:read");

        // Add customers:read to the role
        int customersReadPermissionId = await ReadDbContextAsync(async ctx =>
        {
            Permission? perm = await ctx.Permissions
                .FirstOrDefaultAsync(p => p.Resource == "customers" && p.Action == "read");
            return perm!.Id;
        });
        AssignPermissionsRequest secondPermRequest = new() { PermissionIds = [customersReadPermissionId] };
        HttpResponseMessage assignSecondResponse = await adminClient
            .PostAsJsonAsync($"/api/v1/roles/{roleId}/permissions", secondPermRequest);
        assignSecondResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act 2 — user should now have both permissions
        HttpResponseMessage afterResponse = await adminClient
            .GetAsync($"/api/v1/users/{userId}/permissions");
        UserPermissionsResponse? afterBody = await afterResponse.Content
            .ReadFromJsonAsync<UserPermissionsResponse>();

        // Assert
        afterBody!.Permissions.Should().HaveCount(2);
        afterBody.Permissions.Should().Contain("audit:read");
        afterBody.Permissions.Should().Contain("customers:read");
    }
}
