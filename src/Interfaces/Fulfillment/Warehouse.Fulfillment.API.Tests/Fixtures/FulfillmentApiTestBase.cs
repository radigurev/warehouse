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
using Microsoft.IdentityModel.Tokens;
using Warehouse.Fulfillment.DBModel;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Tests.Fixtures;

/// <summary>
/// Base class for Fulfillment API integration tests providing WebApplicationFactory setup,
/// in-memory database, JWT token generation, and helper methods for creating test data via the API.
/// <para>Links to specification SDD-FULF-001.</para>
/// </summary>
public abstract class FulfillmentApiTestBase
{
    private const string TestJwtSecret = "IntegrationTestSecretKey_AtLeast32Characters!";
    private const string TestJwtIssuer = "Warehouse.Auth.API";
    private const string TestJwtAudience = "Warehouse";

    private static int _orderSequence;

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
        DatabaseName = $"FulfillmentTestDb_{Guid.NewGuid():N}";

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
                        d => d.ServiceType == typeof(DbContextOptions<FulfillmentDbContext>));
                    if (dbContextDescriptor is not null)
                        services.Remove(dbContextDescriptor);

                    services.AddDbContext<FulfillmentDbContext>(options =>
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
        FulfillmentDbContext context = scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();
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
    /// Creates a sales order via the API and returns the response message.
    /// </summary>
    protected async Task<HttpResponseMessage> CreateSalesOrderViaApiAsync(
        HttpClient client,
        int customerId = 1,
        int warehouseId = 1,
        int productId = 100,
        decimal orderedQuantity = 10m,
        decimal unitPrice = 25m)
    {
        int sequence = Interlocked.Increment(ref _orderSequence);
        CreateSalesOrderRequest request = new()
        {
            CustomerId = customerId,
            WarehouseId = warehouseId,
            ShippingStreetLine1 = $"123 Main St #{sequence}",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = productId, OrderedQuantity = orderedQuantity, UnitPrice = unitPrice }]
        };

        return await client.PostAsJsonAsync("/api/v1/sales-orders", request);
    }

    /// <summary>
    /// Creates a sales order via the API and returns the deserialized detail DTO.
    /// </summary>
    protected async Task<SalesOrderDetailDto> CreateSalesOrderAndReadAsync(
        HttpClient client,
        int customerId = 1,
        int warehouseId = 1,
        int productId = 100,
        decimal orderedQuantity = 10m,
        decimal unitPrice = 25m)
    {
        HttpResponseMessage response = await CreateSalesOrderViaApiAsync(client, customerId, warehouseId, productId, orderedQuantity, unitPrice);
        response.EnsureSuccessStatusCode();
        SalesOrderDetailDto dto = (await response.Content.ReadFromJsonAsync<SalesOrderDetailDto>())!;
        return dto;
    }

    /// <summary>
    /// Creates a carrier via the API and returns the response message.
    /// </summary>
    protected async Task<HttpResponseMessage> CreateCarrierViaApiAsync(
        HttpClient client,
        string? code = null,
        string name = "DHL Express")
    {
        int sequence = Interlocked.Increment(ref _orderSequence);
        string effectiveCode = code ?? $"CAR-{sequence:D4}";

        CreateCarrierRequest request = new()
        {
            Code = effectiveCode,
            Name = name
        };

        return await client.PostAsJsonAsync("/api/v1/carriers", request);
    }

    /// <summary>
    /// Creates a carrier via the API and returns the deserialized DTO.
    /// </summary>
    protected async Task<CarrierDto> CreateCarrierAndReadAsync(
        HttpClient client,
        string? code = null,
        string name = "DHL Express")
    {
        HttpResponseMessage response = await CreateCarrierViaApiAsync(client, code, name);
        response.EnsureSuccessStatusCode();
        CarrierDto dto = (await response.Content.ReadFromJsonAsync<CarrierDto>())!;
        return dto;
    }

    /// <summary>
    /// Creates a customer return via the API and returns the response message.
    /// </summary>
    protected async Task<HttpResponseMessage> CreateCustomerReturnViaApiAsync(
        HttpClient client,
        int customerId = 1,
        int productId = 100,
        int warehouseId = 1,
        decimal quantity = 5m,
        string reason = "Damaged in transit")
    {
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = customerId,
            Reason = reason,
            Lines = [new CreateCustomerReturnLineRequest { ProductId = productId, WarehouseId = warehouseId, Quantity = quantity }]
        };

        return await client.PostAsJsonAsync("/api/v1/customer-returns", request);
    }

    /// <summary>
    /// Provides access to a scoped DbContext for direct database operations in tests.
    /// </summary>
    protected async Task WithDbContextAsync(Func<FulfillmentDbContext, Task> action)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        FulfillmentDbContext context = scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();
        await action(context);
    }

    /// <summary>
    /// Reads the DbContext with a return value for assertions.
    /// </summary>
    protected async Task<T> ReadDbContextAsync<T>(Func<FulfillmentDbContext, Task<T>> action)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        FulfillmentDbContext context = scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();
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
