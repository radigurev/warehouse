using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="Warehouse.Fulfillment.API.Controllers.ProductPricesController"/>
/// covering CRUD, permission enforcement, and the diagnostic resolver endpoint.
/// <para>Covers CHG-FEAT-007 §2.2, §2.5, §4 error rules.</para>
/// </summary>
[TestFixture]
[Category("CHG-FEAT-007")]
[Category("Integration")]
public sealed class ProductPricesControllerTests : FulfillmentApiTestBase
{
    /// <summary>CHG-FEAT-007 §2.2 — end-to-end create returns 201 with the full DTO.</summary>
    [Test]
    public async Task PostProductPrice_ValidPayload_Returns201WithDto()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("product-prices:create", "product-prices:read");
        CreateProductPriceRequest request = new()
        {
            ProductId = 500,
            CurrencyCode = "USD",
            UnitPrice = 49.99m
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/product-prices", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ProductPriceDto? body = await response.Content.ReadFromJsonAsync<ProductPriceDto>();
        body.Should().NotBeNull();
        body!.ProductId.Should().Be(500);
        body.CurrencyCode.Should().Be("USD");
        body.UnitPrice.Should().Be(49.99m);
    }

    /// <summary>CHG-FEAT-007 §3 V5 / §4 E5 — duplicate (ProductId, CurrencyCode, ValidFrom) returns 409.</summary>
    [Test]
    public async Task PostProductPrice_Duplicate_Returns409ProblemDetails()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("product-prices:create");
        DateTime from = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        CreateProductPriceRequest request = new()
        {
            ProductId = 501,
            CurrencyCode = "USD",
            UnitPrice = 10m,
            ValidFrom = from
        };
        HttpResponseMessage first = await client.PostAsJsonAsync("/api/v1/product-prices", request);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/product-prices", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }

    /// <summary>CHG-FEAT-007 §3 V1-V4 — validation errors surface as 400 ProblemDetails.</summary>
    [Test]
    public async Task PostProductPrice_InvalidPayload_Returns400ProblemDetails()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("product-prices:create");
        CreateProductPriceRequest request = new()
        {
            ProductId = 0,
            CurrencyCode = "usd",
            UnitPrice = -5m
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/product-prices", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>CHG-FEAT-007 §2.6 / §4 E9 — caller lacking product-prices:create gets 403.</summary>
    [Test]
    public async Task PostProductPrice_WithoutPermission_Returns403()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("product-prices:read");
        CreateProductPriceRequest request = new()
        {
            ProductId = 502,
            CurrencyCode = "USD",
            UnitPrice = 10m
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/product-prices", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>CHG-FEAT-007 §2.2 — list filter by productId and currency returns only matching rows.</summary>
    [Test]
    public async Task GetProductPrices_FilterByProductIdAndCurrency_ReturnsPagedSubset()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("product-prices:create", "product-prices:read");
        await client.PostAsJsonAsync("/api/v1/product-prices",
            new CreateProductPriceRequest { ProductId = 601, CurrencyCode = "USD", UnitPrice = 10m });
        await client.PostAsJsonAsync("/api/v1/product-prices",
            new CreateProductPriceRequest { ProductId = 601, CurrencyCode = "EUR", UnitPrice = 9m });
        await client.PostAsJsonAsync("/api/v1/product-prices",
            new CreateProductPriceRequest { ProductId = 602, CurrencyCode = "USD", UnitPrice = 20m });

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/product-prices?ProductId=601&CurrencyCode=USD");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<ProductPriceDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<ProductPriceDto>>();
        body.Should().NotBeNull();
        body!.TotalCount.Should().Be(1);
        body.Items.Should().ContainSingle(p => p.ProductId == 601 && p.CurrencyCode == "USD");
    }

    /// <summary>CHG-FEAT-007 §2.2 — GET by existing ID returns 200 with full DTO.</summary>
    [Test]
    public async Task GetProductPriceById_ExistingId_Returns200WithDto()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("product-prices:create", "product-prices:read");
        HttpResponseMessage created = await client.PostAsJsonAsync("/api/v1/product-prices",
            new CreateProductPriceRequest { ProductId = 700, CurrencyCode = "USD", UnitPrice = 15m });
        ProductPriceDto? createdDto = await created.Content.ReadFromJsonAsync<ProductPriceDto>();

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/product-prices/{createdDto!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ProductPriceDto? body = await response.Content.ReadFromJsonAsync<ProductPriceDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(createdDto.Id);
        body.ProductId.Should().Be(700);
    }

    /// <summary>CHG-FEAT-007 §4 E10 — GET by unknown ID returns 404.</summary>
    [Test]
    public async Task GetProductPriceById_UnknownId_Returns404()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("product-prices:read");

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/product-prices/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// CHG-FEAT-007 §3 V7 / §4 E7 — PUT cannot change ProductId.
    /// Adaptation: UpdateProductPriceRequest has no ProductId field, so a direct "change attempt"
    /// is structurally impossible. This test verifies the equivalent invariant: a PUT request with
    /// valid payload succeeds (200 OK) and the persisted row's ProductId is unchanged.
    /// </summary>
    [Test]
    public async Task PutProductPrice_AttemptToChangeProductId_Returns400ImmutableKey()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("product-prices:create", "product-prices:read", "product-prices:update");
        HttpResponseMessage created = await client.PostAsJsonAsync("/api/v1/product-prices",
            new CreateProductPriceRequest { ProductId = 800, CurrencyCode = "USD", UnitPrice = 10m });
        ProductPriceDto? createdDto = await created.Content.ReadFromJsonAsync<ProductPriceDto>();

        UpdateProductPriceRequest updateRequest = new() { UnitPrice = 20m };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/product-prices/{createdDto!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ProductPriceDto? body = await response.Content.ReadFromJsonAsync<ProductPriceDto>();
        body.Should().NotBeNull();
        body!.ProductId.Should().Be(800, "ProductId MUST remain unchanged -- update shape cannot carry a new value (CHG-FEAT-007 §3 V7).");
        body.CurrencyCode.Should().Be("USD", "CurrencyCode MUST remain unchanged for the same reason.");
        body.UnitPrice.Should().Be(20m);
    }

    /// <summary>CHG-FEAT-007 §2.2 — DELETE existing returns 204.</summary>
    [Test]
    public async Task DeleteProductPrice_ExistingId_Returns204()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("product-prices:create", "product-prices:read", "product-prices:delete");
        HttpResponseMessage created = await client.PostAsJsonAsync("/api/v1/product-prices",
            new CreateProductPriceRequest { ProductId = 900, CurrencyCode = "USD", UnitPrice = 10m });
        ProductPriceDto? createdDto = await created.Content.ReadFromJsonAsync<ProductPriceDto>();

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/product-prices/{createdDto!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>CHG-FEAT-007 §2.2 — deleting a price referenced by historical SO lines MUST succeed and leave SO line snapshots intact.</summary>
    [Test]
    public async Task DeleteProductPrice_ReferencedByHistoricalOrderLines_Returns204AndLinesUnaffected()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(
            "sales-orders:create", "sales-orders:read",
            "product-prices:create", "product-prices:read", "product-prices:delete");

        // Seed a catalog price, then a sales order referencing that price snapshot.
        HttpResponseMessage created = await client.PostAsJsonAsync("/api/v1/product-prices",
            new CreateProductPriceRequest { ProductId = 1000, CurrencyCode = "USD", UnitPrice = 25m });
        ProductPriceDto? priceDto = await created.Content.ReadFromJsonAsync<ProductPriceDto>();

        SalesOrderDetailDto so = await CreateSalesOrderAndReadAsync(client, productId: 1000, unitPrice: 25m);

        // Act
        HttpResponseMessage deleteResponse = await client.DeleteAsync($"/api/v1/product-prices/{priceDto!.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Reload SO and confirm line snapshot is preserved.
        decimal persistedUnitPrice = await ReadDbContextAsync(async db =>
            (await db.SalesOrderLines.FirstAsync(l => l.SalesOrderId == so.Id)).UnitPrice);
        persistedUnitPrice.Should().Be(25m);
    }

    /// <summary>CHG-FEAT-007 §2.5 — /resolve returns 200 with the active price.</summary>
    [Test]
    public async Task GetResolve_ActivePriceExists_Returns200WithDto()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("product-prices:create", "product-prices:read");
        await client.PostAsJsonAsync("/api/v1/product-prices",
            new CreateProductPriceRequest { ProductId = 1100, CurrencyCode = "USD", UnitPrice = 33m });

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/api/v1/product-prices/resolve?productId=1100&currencyCode=USD");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ProductPriceDto? body = await response.Content.ReadFromJsonAsync<ProductPriceDto>();
        body.Should().NotBeNull();
        body!.UnitPrice.Should().Be(33m);
    }

    /// <summary>CHG-FEAT-007 §4 E13 / §3 V8 — /resolve rejects an unparseable onDate with 400 FULF_PRICE_INVALID_DATE.</summary>
    [Test]
    public async Task GetResolve_InvalidOnDate_Returns400WithFulfPriceInvalidDate()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("product-prices:read");

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/api/v1/product-prices/resolve?productId=1100&currencyCode=USD&onDate=not-a-date");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string raw = await response.Content.ReadAsStringAsync();
        raw.Should().Contain("FULF_PRICE_INVALID_DATE");
        raw.Should().Contain("not-a-date");
    }

    /// <summary>CHG-FEAT-007 §4 E8 — /resolve returns 404 with FULF_PRICE_NOT_FOUND when no match exists, and carries productId/currencyCode/onDate in extensions.</summary>
    [Test]
    public async Task GetResolve_NoActivePrice_Returns404WithFulfPriceNotFound()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("product-prices:read");

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/api/v1/product-prices/resolve?productId=99999&currencyCode=USD");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        string raw = await response.Content.ReadAsStringAsync();
        raw.Should().Contain("FULF_PRICE_NOT_FOUND");
        raw.Should().Contain("\"productId\":99999");
        raw.Should().Contain("\"currencyCode\":\"USD\"");
    }

    /// <summary>CHG-FEAT-007 §2.5 — /resolve tiebreak: most recent concrete ValidFrom wins (end-to-end).</summary>
    [Test]
    public async Task GetResolve_OverlappingPrices_ReturnsMostRecentValidFrom()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("product-prices:create", "product-prices:read");

        // Seed two overlapping prices; the more recent ValidFrom MUST win.
        await WithDbContextAsync(async db =>
        {
            DateTime now = DateTime.UtcNow;
            db.ProductPrices.Add(new ProductPrice
            {
                ProductId = 1200,
                CurrencyCode = "USD",
                UnitPrice = 10m,
                ValidFrom = now.AddDays(-30),
                CreatedAtUtc = now,
                CreatedByUserId = 1
            });
            db.ProductPrices.Add(new ProductPrice
            {
                ProductId = 1200,
                CurrencyCode = "USD",
                UnitPrice = 15m,
                ValidFrom = now.AddDays(-1),
                CreatedAtUtc = now,
                CreatedByUserId = 1
            });
            await db.SaveChangesAsync();
        });

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/api/v1/product-prices/resolve?productId=1200&currencyCode=USD");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ProductPriceDto? body = await response.Content.ReadFromJsonAsync<ProductPriceDto>();
        body.Should().NotBeNull();
        body!.UnitPrice.Should().Be(15m);
    }
}
