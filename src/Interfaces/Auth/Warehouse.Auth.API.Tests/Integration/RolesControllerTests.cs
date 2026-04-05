using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Auth.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses.Auth;

namespace Warehouse.Auth.API.Tests.Integration;

/// <summary>
/// Integration tests for the RolesController: role CRUD and deletion constraints.
/// <para>Covers SDD-AUTH-001 sections 2.3 (Authorization/RBAC) and error rules E9, E12, E15, E16.</para>
/// </summary>
[TestFixture]
[Category("SDD-AUTH-001")]
[Category("Integration")]
public sealed class RolesControllerTests : AuthApiTestBase
{
    [Test]
    public async Task GetRoles_Returns200()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        IReadOnlyList<RoleDto>? body = await response.Content.ReadFromJsonAsync<IReadOnlyList<RoleDto>>();
        body.Should().NotBeNull();
        body.Should().Contain(r => r.Name == "Admin");
    }

    [Test]
    public async Task CreateRole_ValidPayload_Returns201()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        CreateRoleRequest request = new() { Name = "Manager", Description = "Manager role" };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        RoleDetailDto? body = await response.Content.ReadFromJsonAsync<RoleDetailDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Manager");
        body.Description.Should().Be("Manager role");
        body.IsSystem.Should().BeFalse();
    }

    [Test]
    public async Task CreateRole_DuplicateName_Returns409()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        CreateRoleRequest firstRequest = new() { Name = "Operator" };
        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/v1/roles", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        CreateRoleRequest duplicateRequest = new() { Name = "Operator" };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/roles", duplicateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }

    [Test]
    public async Task DeleteRole_AdminRole_Returns400()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        int adminRoleId = await ReadDbContextAsync(async ctx =>
        {
            Auth.DBModel.Models.Role? adminRole = await ctx.Roles
                .FirstOrDefaultAsync(r => r.Name == "Admin" && r.IsSystem);
            return adminRole!.Id;
        });

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/roles/{adminRoleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(400);
    }

    [Test]
    public async Task DeleteRole_RoleWithUsers_Returns409()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        CreateRoleRequest roleRequest = new() { Name = "Assigned Role" };
        HttpResponseMessage createRoleResponse = await client.PostAsJsonAsync("/api/v1/roles", roleRequest);
        createRoleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        RoleDetailDto? createdRole = await createRoleResponse.Content.ReadFromJsonAsync<RoleDetailDto>();
        int roleId = createdRole!.Id;

        HttpResponseMessage createUserResponse = await CreateUserViaApiAsync(
            client, "roleuser", "roleuser@warehouse.local", "RoleUser1!");
        createUserResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        CreateUserResponse? createdUser = await createUserResponse.Content.ReadFromJsonAsync<CreateUserResponse>();
        int userId = createdUser!.Id;

        AssignRolesRequest assignRequest = new() { RoleIds = [roleId] };
        HttpResponseMessage assignResponse = await client.PostAsJsonAsync($"/api/v1/users/{userId}/roles", assignRequest);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/roles/{roleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }
}
