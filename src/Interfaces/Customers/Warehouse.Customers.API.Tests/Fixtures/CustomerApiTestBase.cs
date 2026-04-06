using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Warehouse.Customers.DBModel;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Customers.API.Tests.Fixtures;

/// <summary>
/// Base class for Customers API integration tests providing WebApplicationFactory setup,
/// in-memory database, JWT token generation, and helper methods for creating test data via the API.
/// <para>Links to specification SDD-CUST-001.</para>
/// </summary>
public abstract class CustomerApiTestBase
{
    private const string TestJwtSecret = "IntegrationTestSecretKey_AtLeast32Characters!";
    private const string TestJwtIssuer = "Warehouse.Auth.API";
    private const string TestJwtAudience = "Warehouse";

    private static int _customerCodeSequence;

    private WebApplicationFactory<Program> _factory = null!;

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
        DatabaseName = $"CustomersTestDb_{Guid.NewGuid():N}";

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.UseSetting("ConnectionStrings:WarehouseDb", "Server=(localdb);Database=Unused;Trusted_Connection=True;");
                builder.UseSetting("Jwt:SecretKey", TestJwtSecret);
                builder.UseSetting("Jwt:Issuer", TestJwtIssuer);
                builder.UseSetting("Jwt:Audience", TestJwtAudience);
                builder.UseSetting("Jwt:AccessTokenExpirationMinutes", "30");
                builder.UseSetting("Jwt:RefreshTokenExpirationDays", "7");

                builder.ConfigureTestServices(services =>
                {
                    ServiceDescriptor? dbContextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<CustomersDbContext>));
                    if (dbContextDescriptor is not null)
                        services.Remove(dbContextDescriptor);

                    services.AddDbContext<CustomersDbContext>(options =>
                        options.UseInMemoryDatabase(DatabaseName)
                            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

                    services.RemoveAll<IHealthCheck>();
                    services.AddMassTransitTestHarness();

                    services.PostConfigure<JwtBearerOptions>(
                        JwtBearerDefaults.AuthenticationScheme,
                        options =>
                        {
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                ValidIssuer = TestJwtIssuer,
                                ValidAudience = TestJwtAudience,
                                IssuerSigningKey = new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(TestJwtSecret)),
                                ClockSkew = TimeSpan.Zero
                            };
                        });
                });
            });

        using IServiceScope scope = _factory.Services.CreateScope();
        CustomersDbContext context = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
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
    /// Creates an HTTP client authenticated with a JWT containing the specified permission claims.
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(params string[] permissions)
    {
        HttpClient client = _factory.CreateClient();
        string token = GenerateJwtToken(userId: 1, permissions: permissions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates a customer via the API and returns the response message.
    /// Generates a unique code automatically if none is provided to avoid InMemory provider limitations.
    /// </summary>
    protected async Task<HttpResponseMessage> CreateCustomerViaApiAsync(
        HttpClient client,
        string name = "Test Customer",
        string? code = null,
        string? taxId = null,
        int? categoryId = null,
        string? notes = null)
    {
        int sequence = Interlocked.Increment(ref _customerCodeSequence);
        string effectiveCode = code ?? $"TST-{sequence:D6}";

        CreateCustomerRequest request = new()
        {
            Name = name,
            Code = effectiveCode,
            TaxId = taxId,
            CategoryId = categoryId,
            Notes = notes
        };

        return await client.PostAsJsonAsync("/api/v1/customers", request);
    }

    /// <summary>
    /// Creates a customer via the API and returns the deserialized detail DTO.
    /// </summary>
    protected async Task<CustomerDetailDto> CreateCustomerAndReadAsync(
        HttpClient client,
        string name = "Test Customer",
        string? code = null,
        string? taxId = null,
        int? categoryId = null)
    {
        HttpResponseMessage response = await CreateCustomerViaApiAsync(client, name, code, taxId, categoryId);
        response.EnsureSuccessStatusCode();
        CustomerDetailDto dto = (await response.Content.ReadFromJsonAsync<CustomerDetailDto>())!;
        return dto;
    }

    /// <summary>
    /// Creates a category via the API and returns the response message.
    /// </summary>
    protected async Task<HttpResponseMessage> CreateCategoryViaApiAsync(
        HttpClient client,
        string name = "Wholesale",
        string? description = null)
    {
        CreateCategoryRequest request = new()
        {
            Name = name,
            Description = description
        };

        return await client.PostAsJsonAsync("/api/v1/customer-categories", request);
    }

    /// <summary>
    /// Creates a category via the API and returns the deserialized DTO.
    /// </summary>
    protected async Task<CustomerCategoryDto> CreateCategoryAndReadAsync(
        HttpClient client,
        string name = "Wholesale",
        string? description = null)
    {
        HttpResponseMessage response = await CreateCategoryViaApiAsync(client, name, description);
        response.EnsureSuccessStatusCode();
        CustomerCategoryDto dto = (await response.Content.ReadFromJsonAsync<CustomerCategoryDto>())!;
        return dto;
    }

    /// <summary>
    /// Creates an account via the API and returns the response message.
    /// </summary>
    protected async Task<HttpResponseMessage> CreateAccountViaApiAsync(
        HttpClient client,
        int customerId,
        string currencyCode = "USD",
        string? description = null)
    {
        CreateAccountRequest request = new()
        {
            CurrencyCode = currencyCode,
            Description = description
        };

        return await client.PostAsJsonAsync($"/api/v1/customers/{customerId}/accounts", request);
    }

    /// <summary>
    /// Creates an account via the API and returns the deserialized DTO.
    /// </summary>
    protected async Task<CustomerAccountDto> CreateAccountAndReadAsync(
        HttpClient client,
        int customerId,
        string currencyCode = "USD",
        string? description = null)
    {
        HttpResponseMessage response = await CreateAccountViaApiAsync(client, customerId, currencyCode, description);
        response.EnsureSuccessStatusCode();
        CustomerAccountDto dto = (await response.Content.ReadFromJsonAsync<CustomerAccountDto>())!;
        return dto;
    }

    /// <summary>
    /// Creates an address via the API and returns the response message.
    /// </summary>
    protected async Task<HttpResponseMessage> CreateAddressViaApiAsync(
        HttpClient client,
        int customerId,
        string addressType = "Billing",
        string streetLine1 = "123 Main St",
        string city = "Sofia",
        string postalCode = "1000",
        string countryCode = "BG")
    {
        CreateAddressRequest request = new()
        {
            AddressType = addressType,
            StreetLine1 = streetLine1,
            City = city,
            PostalCode = postalCode,
            CountryCode = countryCode
        };

        return await client.PostAsJsonAsync($"/api/v1/customers/{customerId}/addresses", request);
    }

    /// <summary>
    /// Creates a phone via the API and returns the response message.
    /// </summary>
    protected async Task<HttpResponseMessage> CreatePhoneViaApiAsync(
        HttpClient client,
        int customerId,
        string phoneType = "Mobile",
        string phoneNumber = "+359888123456")
    {
        CreatePhoneRequest request = new()
        {
            PhoneType = phoneType,
            PhoneNumber = phoneNumber
        };

        return await client.PostAsJsonAsync($"/api/v1/customers/{customerId}/phones", request);
    }

    /// <summary>
    /// Creates an email via the API and returns the response message.
    /// </summary>
    protected async Task<HttpResponseMessage> CreateEmailViaApiAsync(
        HttpClient client,
        int customerId,
        string emailType = "General",
        string emailAddress = "test@example.com")
    {
        CreateEmailRequest request = new()
        {
            EmailType = emailType,
            EmailAddress = emailAddress
        };

        return await client.PostAsJsonAsync($"/api/v1/customers/{customerId}/emails", request);
    }

    /// <summary>
    /// Provides access to a scoped DbContext for direct database operations in tests.
    /// </summary>
    protected async Task WithDbContextAsync(Func<CustomersDbContext, Task> action)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        CustomersDbContext context = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
        await action(context);
    }

    /// <summary>
    /// Reads the DbContext with a return value for assertions.
    /// </summary>
    protected async Task<T> ReadDbContextAsync<T>(Func<CustomersDbContext, Task<T>> action)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        CustomersDbContext context = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
        return await action(context);
    }

    private static string GenerateJwtToken(int userId, string[] permissions)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(TestJwtSecret));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub", userId.ToString())
        ];

        foreach (string permission in permissions)
            claims.Add(new Claim("permission", permission));

        JwtSecurityToken token = new(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        JwtSecurityTokenHandler handler = new();
        return handler.WriteToken(token);
    }
}
