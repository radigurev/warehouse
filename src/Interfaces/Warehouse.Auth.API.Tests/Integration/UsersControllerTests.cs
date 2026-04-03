using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Auth.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Auth;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Auth.API.Tests.Integration;

/// <summary>
/// Integration tests for the UsersController: user CRUD, password change, and role assignment.
/// <para>Covers SDD-AUTH-001 sections 2.1 (User Management) and error rules E8, E10, E11, E13, E14.</para>
/// </summary>
[TestFixture]
[Category("SDD-AUTH-001")]
[Category("Integration")]
public sealed class UsersControllerTests : AuthApiTestBase
{
    [Test]
    public async Task GetUsers_Authenticated_Returns200()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<UserDto>? body = await response.Content.ReadFromJsonAsync<PaginatedResponse<UserDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeEmpty();
        body.TotalCount.Should().BeGreaterOrEqualTo(1);
    }

    [Test]
    public async Task GetUsers_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateUser_ValidPayload_Returns201()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await CreateUserViaApiAsync(
            client, "newuser", "newuser@warehouse.local", "NewPass1!", "New", "User");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        UserDetailDto? body = await response.Content.ReadFromJsonAsync<UserDetailDto>();
        body.Should().NotBeNull();
        body!.Username.Should().Be("newuser");
        body.Email.Should().Be("newuser@warehouse.local");
        body.FirstName.Should().Be("New");
        body.LastName.Should().Be("User");
        body.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task CreateUser_DuplicateUsername_Returns409()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        await CreateUserViaApiAsync(client, "dupuser", "first@warehouse.local", "ValidPass1!");

        // Act
        HttpResponseMessage response = await CreateUserViaApiAsync(
            client, "dupuser", "second@warehouse.local", "ValidPass1!");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }

    [Test]
    public async Task CreateUser_DuplicateEmail_Returns409()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        await CreateUserViaApiAsync(client, "user_one", "same@warehouse.local", "ValidPass1!");

        // Act
        HttpResponseMessage response = await CreateUserViaApiAsync(
            client, "user_two", "same@warehouse.local", "ValidPass1!");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }

    [Test]
    public async Task CreateUser_InvalidPassword_Returns400()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await CreateUserViaApiAsync(
            client, "weakuser", "weak@warehouse.local", "short");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetUserById_ExistingUser_Returns200()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        HttpResponseMessage createResponse = await CreateUserViaApiAsync(client, "findme", "findme@warehouse.local", "FindMe1!");
        UserDetailDto? created = await createResponse.Content.ReadFromJsonAsync<UserDetailDto>();
        int userId = created!.Id;

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        UserDetailDto? body = await response.Content.ReadFromJsonAsync<UserDetailDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(userId);
        body.Username.Should().Be("findme");
    }

    [Test]
    public async Task UpdateUser_ValidPayload_Returns200()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        HttpResponseMessage createResponse = await CreateUserViaApiAsync(
            client, "updateme", "old@warehouse.local", "UpdateMe1!", "Old", "Name");
        UserDetailDto? created = await createResponse.Content.ReadFromJsonAsync<UserDetailDto>();
        int userId = created!.Id;

        UpdateUserRequest updateRequest = new()
        {
            Email = "new@warehouse.local",
            FirstName = "Updated",
            LastName = "Person"
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/users/{userId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        UserDetailDto? body = await response.Content.ReadFromJsonAsync<UserDetailDto>();
        body.Should().NotBeNull();
        body!.Email.Should().Be("new@warehouse.local");
        body.FirstName.Should().Be("Updated");
        body.LastName.Should().Be("Person");
    }

    [Test]
    public async Task DeactivateUser_Returns204()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        HttpResponseMessage createResponse = await CreateUserViaApiAsync(
            client, "byeuser", "bye@warehouse.local", "ByeUser1!");
        UserDetailDto? created = await createResponse.Content.ReadFromJsonAsync<UserDetailDto>();
        int userId = created!.Id;

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeactivatedUser_CannotLogin()
    {
        // Arrange
        HttpClient adminClient = await CreateAuthenticatedClientAsync();
        await CreateUserViaApiAsync(adminClient, "willdie", "willdie@warehouse.local", "WillDie1!");
        UserDetailDto? created = (await (await CreateUserViaApiAsync(
            adminClient, "willdie2", "willdie2@warehouse.local", "WillDie2!"))
            .Content.ReadFromJsonAsync<UserDetailDto>());
        int userId = created!.Id;

        HttpResponseMessage deactivateResponse = await adminClient.DeleteAsync($"/api/v1/users/{userId}");
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpClient anonClient = CreateClient();
        LoginRequest loginRequest = new() { Username = "willdie2", Password = "WillDie2!" };

        // Act
        HttpResponseMessage response = await anonClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
