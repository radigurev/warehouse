using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Auth.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;

namespace Warehouse.Auth.API.Tests.Integration;

/// <summary>
/// Integration tests for the PermissionsController: permission CRUD.
/// <para>Covers SDD-AUTH-001 sections 2.3 (Permissions) and error rule E17.</para>
/// </summary>
[TestFixture]
[Category("SDD-AUTH-001")]
[Category("Integration")]
public sealed class PermissionsControllerTests : AuthApiTestBase
{
    [Test]
    public async Task GetPermissions_Returns200()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        IReadOnlyList<PermissionDto>? body = await response.Content.ReadFromJsonAsync<IReadOnlyList<PermissionDto>>();
        body.Should().NotBeNull();
    }

    [Test]
    public async Task CreatePermission_Returns201()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        CreatePermissionRequest request = new()
        {
            Resource = "inventory",
            Action = "read",
            Description = "Read inventory items"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/permissions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        PermissionDto? body = await response.Content.ReadFromJsonAsync<PermissionDto>();
        body.Should().NotBeNull();
        body!.Resource.Should().Be("inventory");
        body.Action.Should().Be("read");
        body.Description.Should().Be("Read inventory items");
    }

    [Test]
    public async Task CreatePermission_Duplicate_Returns409()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        CreatePermissionRequest request = new() { Resource = "orders", Action = "write" };
        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/v1/permissions", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        CreatePermissionRequest duplicateRequest = new() { Resource = "orders", Action = "write" };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/permissions", duplicateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }
}
