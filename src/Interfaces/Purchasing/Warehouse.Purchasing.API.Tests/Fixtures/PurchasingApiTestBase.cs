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
using Warehouse.Purchasing.DBModel;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Tests.Fixtures;

/// <summary>
/// Base class for Purchasing API integration tests providing WebApplicationFactory setup,
/// in-memory database, JWT token generation, and helper methods for creating test data via the API.
/// <para>Links to specification SDD-PURCH-001.</para>
/// </summary>
public abstract class PurchasingApiTestBase
{
    private const string TestJwtSecret = "IntegrationTestSecretKey_AtLeast32Characters!";
    private const string TestJwtIssuer = "Warehouse.Auth.API";
    private const string TestJwtAudience = "Warehouse";

    private static int _supplierCodeSequence;

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
        DatabaseName = $"PurchasingTestDb_{Guid.NewGuid():N}";

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
                        d => d.ServiceType == typeof(DbContextOptions<PurchasingDbContext>));
                    if (dbContextDescriptor is not null)
                        services.Remove(dbContextDescriptor);

                    services.AddDbContext<PurchasingDbContext>(options =>
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
        PurchasingDbContext context = scope.ServiceProvider.GetRequiredService<PurchasingDbContext>();
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
    /// Creates a supplier category via the API and returns the response message.
    /// </summary>
    protected async Task<HttpResponseMessage> CreateCategoryViaApiAsync(
        HttpClient client,
        string name = "Raw Materials",
        string? description = null)
    {
        CreateSupplierCategoryRequest request = new()
        {
            Name = name,
            Description = description
        };

        return await client.PostAsJsonAsync("/api/v1/supplier-categories", request);
    }

    /// <summary>
    /// Creates a supplier category via the API and returns the deserialized DTO.
    /// </summary>
    protected async Task<SupplierCategoryDto> CreateCategoryAndReadAsync(
        HttpClient client,
        string name = "Raw Materials",
        string? description = null)
    {
        HttpResponseMessage response = await CreateCategoryViaApiAsync(client, name, description);
        response.EnsureSuccessStatusCode();
        SupplierCategoryDto dto = (await response.Content.ReadFromJsonAsync<SupplierCategoryDto>())!;
        return dto;
    }

    /// <summary>
    /// Creates a supplier via the API and returns the response message.
    /// </summary>
    protected async Task<HttpResponseMessage> CreateSupplierViaApiAsync(
        HttpClient client,
        string name = "Test Supplier",
        string? code = null,
        string? taxId = null,
        int? categoryId = null,
        string? notes = null)
    {
        int sequence = Interlocked.Increment(ref _supplierCodeSequence);
        string effectiveCode = code ?? $"SUPP-{sequence:D6}";

        CreateSupplierRequest request = new()
        {
            Name = name,
            Code = effectiveCode,
            TaxId = taxId,
            CategoryId = categoryId,
            Notes = notes
        };

        return await client.PostAsJsonAsync("/api/v1/suppliers", request);
    }

    /// <summary>
    /// Creates a supplier via the API and returns the deserialized detail DTO.
    /// </summary>
    protected async Task<SupplierDetailDto> CreateSupplierAndReadAsync(
        HttpClient client,
        string name = "Test Supplier",
        string? code = null,
        string? taxId = null,
        int? categoryId = null)
    {
        HttpResponseMessage response = await CreateSupplierViaApiAsync(client, name, code, taxId, categoryId);
        response.EnsureSuccessStatusCode();
        SupplierDetailDto dto = (await response.Content.ReadFromJsonAsync<SupplierDetailDto>())!;
        return dto;
    }

    /// <summary>
    /// Creates a purchase order via the API and returns the response message.
    /// </summary>
    protected async Task<HttpResponseMessage> CreatePurchaseOrderViaApiAsync(
        HttpClient client,
        int supplierId,
        int destinationWarehouseId = 1,
        int productId = 100,
        decimal orderedQuantity = 50m,
        decimal unitPrice = 10m)
    {
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = supplierId,
            DestinationWarehouseId = destinationWarehouseId,
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = productId, OrderedQuantity = orderedQuantity, UnitPrice = unitPrice }]
        };

        return await client.PostAsJsonAsync("/api/v1/purchase-orders", request);
    }

    /// <summary>
    /// Creates a purchase order via the API and returns the deserialized detail DTO.
    /// </summary>
    protected async Task<PurchaseOrderDetailDto> CreatePurchaseOrderAndReadAsync(
        HttpClient client,
        int supplierId,
        int destinationWarehouseId = 1,
        int productId = 100,
        decimal orderedQuantity = 50m,
        decimal unitPrice = 10m)
    {
        HttpResponseMessage response = await CreatePurchaseOrderViaApiAsync(client, supplierId, destinationWarehouseId, productId, orderedQuantity, unitPrice);
        response.EnsureSuccessStatusCode();
        PurchaseOrderDetailDto dto = (await response.Content.ReadFromJsonAsync<PurchaseOrderDetailDto>())!;
        return dto;
    }

    /// <summary>
    /// Creates a goods receipt via the API and returns the response message.
    /// </summary>
    protected async Task<HttpResponseMessage> CreateGoodsReceiptViaApiAsync(
        HttpClient client,
        int purchaseOrderId,
        int purchaseOrderLineId,
        decimal receivedQuantity = 10m,
        int warehouseId = 1)
    {
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = purchaseOrderId,
            WarehouseId = warehouseId,
            Lines = [new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = purchaseOrderLineId, ReceivedQuantity = receivedQuantity }]
        };

        return await client.PostAsJsonAsync("/api/v1/goods-receipts", request);
    }

    /// <summary>
    /// Creates a supplier return via the API and returns the response message.
    /// </summary>
    protected async Task<HttpResponseMessage> CreateSupplierReturnViaApiAsync(
        HttpClient client,
        int supplierId,
        int productId = 100,
        int warehouseId = 1,
        decimal quantity = 5m,
        string reason = "Defective goods")
    {
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = supplierId,
            Reason = reason,
            Lines = [new CreateSupplierReturnLineRequest { ProductId = productId, WarehouseId = warehouseId, Quantity = quantity }]
        };

        return await client.PostAsJsonAsync("/api/v1/supplier-returns", request);
    }

    /// <summary>
    /// Provides access to a scoped DbContext for direct database operations in tests.
    /// </summary>
    protected async Task WithDbContextAsync(Func<PurchasingDbContext, Task> action)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        PurchasingDbContext context = scope.ServiceProvider.GetRequiredService<PurchasingDbContext>();
        await action(context);
    }

    /// <summary>
    /// Reads the DbContext with a return value for assertions.
    /// </summary>
    protected async Task<T> ReadDbContextAsync<T>(Func<PurchasingDbContext, Task<T>> action)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        PurchasingDbContext context = scope.ServiceProvider.GetRequiredService<PurchasingDbContext>();
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
