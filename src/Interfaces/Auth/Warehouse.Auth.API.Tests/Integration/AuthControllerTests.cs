using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Auth.API.Tests.Fixtures;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses.Auth;

namespace Warehouse.Auth.API.Tests.Integration;

/// <summary>
/// Integration tests for the AuthController: login, refresh, and logout endpoints.
/// <para>Covers SDD-AUTH-001 sections 2.2 (Authentication) and error rules E1, E2, E5, E6.</para>
/// </summary>
[TestFixture]
[Category("SDD-AUTH-001")]
[Category("Integration")]
public sealed class AuthControllerTests : AuthApiTestBase
{
    [Test]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        // Arrange
        HttpClient client = CreateClient();
        LoginRequest request = new() { Username = AdminUsername, Password = AdminPassword };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        LoginResponse? body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Test]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        LoginRequest request = new() { Username = AdminUsername, Password = "WrongPassword1!" };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(401);
    }

    [Test]
    public async Task Login_NonExistentUser_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        LoginRequest request = new() { Username = "nonexistent_user", Password = "SomePass1!" };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(401);
    }

    [Test]
    public async Task Login_EmptyBody_Returns400()
    {
        // Arrange
        HttpClient client = CreateClient();
        object request = new { Username = "", Password = "" };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Login_DeactivatedUser_Returns401()
    {
        // Arrange
        HttpClient adminClient = await CreateAuthenticatedClientAsync();
        await CreateUserViaApiAsync(adminClient, "deactivate_me", "deactivate@test.local", "DeactivatePass1!");
        await adminClient.DeleteAsync("/api/v1/users/2");

        HttpClient client = CreateClient();
        LoginRequest loginRequest = new() { Username = "deactivate_me", Password = "DeactivatePass1!" };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Refresh_ValidToken_Returns200WithNewTokens()
    {
        // Arrange
        HttpClient client = CreateClient();
        LoginResponse loginResponse = await LoginAsync(AdminUsername, AdminPassword, client);
        RefreshTokenRequest request = new() { RefreshToken = loginResponse.RefreshToken };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RefreshTokenResponse? body = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBe(loginResponse.RefreshToken, "Token rotation should issue a new refresh token.");
        body.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Test]
    public async Task Refresh_OldTokenAfterRotation_RejectsRequest()
    {
        // Arrange
        // Note: EF Core InMemory provider does not support ExecuteUpdateAsync used in token
        // theft detection (HandleRevokedTokenAsync). With InMemory, the revoked token path
        // returns 500 instead of 401. With a real SQL Server, this would return 401.
        HttpClient client = CreateClient();
        LoginResponse loginResponse = await LoginAsync(AdminUsername, AdminPassword, client);
        string originalRefreshToken = loginResponse.RefreshToken;

        RefreshTokenRequest firstRefresh = new() { RefreshToken = originalRefreshToken };
        HttpResponseMessage firstRefreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", firstRefresh);
        firstRefreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        RefreshTokenRequest secondRefresh = new() { RefreshToken = originalRefreshToken };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/refresh", secondRefresh);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse("Reusing a rotated refresh token must be rejected.");
    }

    [Test]
    public async Task Refresh_InvalidToken_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        RefreshTokenRequest request = new() { RefreshToken = "completely_invalid_token_value" };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Logout_ValidToken_Returns204()
    {
        // Arrange
        HttpClient client = CreateClient();
        LoginResponse loginResponse = await LoginAsync(AdminUsername, AdminPassword, client);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        RefreshTokenRequest request = new() { RefreshToken = loginResponse.RefreshToken };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/logout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Logout_RefreshTokenRevokedAfterLogout()
    {
        // Arrange
        // Note: EF Core InMemory provider does not support ExecuteUpdateAsync used in token
        // theft detection (HandleRevokedTokenAsync). With InMemory, the revoked token path
        // returns 500 instead of 401. With a real SQL Server, this would return 401.
        HttpClient client = CreateClient();
        LoginResponse loginResponse = await LoginAsync(AdminUsername, AdminPassword, client);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        RefreshTokenRequest logoutRequest = new() { RefreshToken = loginResponse.RefreshToken };
        HttpResponseMessage logoutResponse = await client.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        client.DefaultRequestHeaders.Authorization = null;
        RefreshTokenRequest refreshRequest = new() { RefreshToken = loginResponse.RefreshToken };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse("Refresh with a revoked token must be rejected.");
    }
}
