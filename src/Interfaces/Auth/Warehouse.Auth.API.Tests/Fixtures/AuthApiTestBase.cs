using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Warehouse.Auth.DBModel;
using Warehouse.ServiceModel.Requests.Auth;
using Warehouse.ServiceModel.Responses.Auth;

namespace Warehouse.Auth.API.Tests.Fixtures;

/// <summary>
/// Base class for Auth API integration tests providing WebApplicationFactory setup,
/// in-memory database, seeding helpers, and authenticated HTTP client creation.
/// <para>Links to specification SDD-AUTH-001.</para>
/// </summary>
public abstract class AuthApiTestBase
{
    private WebApplicationFactory<Program> _factory = null!;

    /// <summary>
    /// Gets the default admin username seeded in the database.
    /// </summary>
    protected const string AdminUsername = "admin";

    /// <summary>
    /// Gets the default admin password seeded in the database.
    /// </summary>
    protected const string AdminPassword = "Admin123!";

    /// <summary>
    /// Gets the unique database name for test isolation.
    /// </summary>
    protected string DatabaseName { get; private set; } = null!;

    /// <summary>
    /// Creates the WebApplicationFactory and seeds the database for each test.
    /// </summary>
    [SetUp]
    public virtual async Task SetUpAsync()
    {
        DatabaseName = $"AuthTestDb_{Guid.NewGuid():N}";

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["FeatureManagement:EnableDatabaseSeeding"] = "true",
                        ["FeatureManagement:EnableSeedDefaultAdmin"] = "true"
                    });
                });
                builder.ConfigureTestServices(services =>
                {
                    ServiceDescriptor? dbContextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));
                    if (dbContextDescriptor is not null)
                        services.Remove(dbContextDescriptor);

                    services.AddDbContext<AuthDbContext>(options =>
                        options.UseInMemoryDatabase(DatabaseName));

                    services.RemoveAll<IHealthCheck>();
                });
            });

        using IServiceScope scope = _factory.Services.CreateScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Disposes the factory after each test.
    /// </summary>
    [TearDown]
    public virtual void TearDown()
    {
        _factory?.Dispose();
    }

    /// <summary>
    /// Creates an anonymous (unauthenticated) HTTP client.
    /// </summary>
    protected HttpClient CreateClient()
    {
        return _factory.CreateClient();
    }

    /// <summary>
    /// Creates an HTTP client authenticated as the seeded admin user.
    /// </summary>
    protected async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        HttpClient client = _factory.CreateClient();
        string accessToken = await GetAccessTokenAsync(AdminUsername, AdminPassword, client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>
    /// Creates an HTTP client authenticated with the specified credentials.
    /// </summary>
    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string username, string password)
    {
        HttpClient client = _factory.CreateClient();
        string accessToken = await GetAccessTokenAsync(username, password, client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>
    /// Logs in with the specified credentials and returns a LoginResponse.
    /// </summary>
    protected async Task<LoginResponse> LoginAsync(string username, string password, HttpClient? client = null)
    {
        HttpClient httpClient = client ?? CreateClient();
        LoginRequest request = new() { Username = username, Password = password };
        HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/v1/auth/login", request);
        response.EnsureSuccessStatusCode();
        LoginResponse loginResponse = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        return loginResponse;
    }

    /// <summary>
    /// Gets a JWT access token for the specified user.
    /// </summary>
    protected async Task<string> GetAccessTokenAsync(string username, string password, HttpClient? client = null)
    {
        LoginResponse loginResponse = await LoginAsync(username, password, client);
        return loginResponse.AccessToken;
    }

    /// <summary>
    /// Creates a new test user via the API and returns the response message.
    /// </summary>
    protected async Task<HttpResponseMessage> CreateUserViaApiAsync(
        HttpClient client,
        string username = "testuser",
        string email = "test@warehouse.local",
        string password = "TestPass1!",
        string firstName = "Test",
        string lastName = "User")
    {
        CreateUserRequest request = new()
        {
            Username = username,
            Email = email,
            Password = password,
            FirstName = firstName,
            LastName = lastName
        };

        return await client.PostAsJsonAsync("/api/v1/users", request);
    }

    /// <summary>
    /// Provides access to a scoped DbContext for direct database operations in tests.
    /// </summary>
    protected async Task WithDbContextAsync(Func<AuthDbContext, Task> action)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await action(context);
    }

    /// <summary>
    /// Reads the DbContext with a return value for assertions.
    /// </summary>
    protected async Task<T> ReadDbContextAsync<T>(Func<AuthDbContext, Task<T>> action)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await action(context);
    }
}
