using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Tests.Integration;

/// <summary>
/// Integration tests for SalesOrdersController covering CRUD, lifecycle transitions, and line management.
/// <para>Covers SDD-FULF-001 sections on sales order lifecycle and validation.</para>
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
[Category("Integration")]
public sealed class SalesOrdersControllerTests : FulfillmentApiTestBase
{
    [Test]
    public async Task Create_Valid_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read");

        // Act
        HttpResponseMessage response = await CreateSalesOrderViaApiAsync(client);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SalesOrderDetailDto? body = await response.Content.ReadFromJsonAsync<SalesOrderDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Draft");
        body.Lines.Should().HaveCount(1);
        body.CustomerId.Should().Be(1);
    }

    [Test]
    public async Task Create_EmptyLines_Returns400()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create");
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 1,
            CurrencyCode = "USD",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = []
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/sales-orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_InvalidPayload_Returns400()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create");
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 0,
            CustomerAccountId = 0,
            CurrencyCode = "",
            WarehouseId = 0,
            ShippingStreetLine1 = "",
            ShippingCity = "",
            ShippingPostalCode = "",
            ShippingCountryCode = "",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 0, OrderedQuantity = -1, UnitPrice = -1 }]
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/sales-orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Get_Existing_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read");
        SalesOrderDetailDto created = await CreateSalesOrderAndReadAsync(client);

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/sales-orders/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SalesOrderDetailDto? body = await response.Content.ReadFromJsonAsync<SalesOrderDetailDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
        body.OrderNumber.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Get_NonExistent_Returns404()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:read");

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/sales-orders/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Search_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read");
        await CreateSalesOrderAndReadAsync(client);
        await CreateSalesOrderAndReadAsync(client);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/sales-orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<SalesOrderDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<SalesOrderDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Test]
    public async Task Search_ByStatus_ReturnsFiltered()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        SalesOrderDetailDto draft = await CreateSalesOrderAndReadAsync(client);
        SalesOrderDetailDto toConfirm = await CreateSalesOrderAndReadAsync(client);
        await client.PostAsync($"/api/v1/sales-orders/{toConfirm.Id}/confirm", null);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/sales-orders?Status=Confirmed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<SalesOrderDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<SalesOrderDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().OnlyContain(so => so.Status == "Confirmed");
    }

    [Test]
    public async Task Update_Draft_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        SalesOrderDetailDto created = await CreateSalesOrderAndReadAsync(client);
        UpdateSalesOrderRequest request = new()
        {
            WarehouseId = 1,
            ShippingStreetLine1 = "456 Updated Ave",
            ShippingCity = "Chicago",
            ShippingPostalCode = "60601",
            ShippingCountryCode = "US",
            Notes = "Updated notes"
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/sales-orders/{created.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SalesOrderDetailDto? body = await response.Content.ReadFromJsonAsync<SalesOrderDetailDto>();
        body.Should().NotBeNull();
        body!.ShippingCity.Should().Be("Chicago");
        body.Notes.Should().Be("Updated notes");
    }

    [Test]
    public async Task Confirm_Draft_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        SalesOrderDetailDto created = await CreateSalesOrderAndReadAsync(client);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/sales-orders/{created.Id}/confirm", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SalesOrderDetailDto? body = await response.Content.ReadFromJsonAsync<SalesOrderDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Confirmed");
        body.ConfirmedAtUtc.Should().NotBeNull();
    }

    [Test]
    public async Task Confirm_NonDraft_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        SalesOrderDetailDto created = await CreateSalesOrderAndReadAsync(client);
        await client.PostAsync($"/api/v1/sales-orders/{created.Id}/confirm", null);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/sales-orders/{created.Id}/confirm", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Cancel_Draft_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        SalesOrderDetailDto created = await CreateSalesOrderAndReadAsync(client);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/sales-orders/{created.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SalesOrderDetailDto? body = await response.Content.ReadFromJsonAsync<SalesOrderDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Cancelled");
    }

    [Test]
    public async Task Cancel_NonDraftOrWithPicks_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        SalesOrderDetailDto created = await CreateSalesOrderAndReadAsync(client);
        await client.PostAsync($"/api/v1/sales-orders/{created.Id}/cancel", null);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/sales-orders/{created.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task AddLine_Draft_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        SalesOrderDetailDto created = await CreateSalesOrderAndReadAsync(client);
        await EnsureProductPriceAsync(200, "USD", 15m);
        CreateSalesOrderLineRequest lineRequest = new()
        {
            ProductId = 200,
            OrderedQuantity = 5m,
            UnitPrice = 15m
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/sales-orders/{created.Id}/lines", lineRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SalesOrderLineDto? body = await response.Content.ReadFromJsonAsync<SalesOrderLineDto>();
        body.Should().NotBeNull();
        body!.ProductId.Should().Be(200);
    }

    [Test]
    public async Task AddLine_NonDraft_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        SalesOrderDetailDto created = await CreateSalesOrderAndReadAsync(client);
        await client.PostAsync($"/api/v1/sales-orders/{created.Id}/confirm", null);
        CreateSalesOrderLineRequest lineRequest = new()
        {
            ProductId = 200,
            OrderedQuantity = 5m,
            UnitPrice = 15m
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/sales-orders/{created.Id}/lines", lineRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task AddLine_DuplicateProduct_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        SalesOrderDetailDto created = await CreateSalesOrderAndReadAsync(client, productId: 100);
        CreateSalesOrderLineRequest lineRequest = new()
        {
            ProductId = 100,
            OrderedQuantity = 5m,
            UnitPrice = 15m
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/sales-orders/{created.Id}/lines", lineRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task UpdateLine_Valid_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        SalesOrderDetailDto created = await CreateSalesOrderAndReadAsync(client);
        int lineId = created.Lines[0].Id;
        UpdateSalesOrderLineRequest lineRequest = new()
        {
            OrderedQuantity = 20m,
            UnitPrice = 30m,
            Notes = "Updated line"
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/sales-orders/{created.Id}/lines/{lineId}", lineRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SalesOrderLineDto? body = await response.Content.ReadFromJsonAsync<SalesOrderLineDto>();
        body.Should().NotBeNull();
        body!.OrderedQuantity.Should().Be(20m);
        body.UnitPrice.Should().Be(30m);
    }

    [Test]
    public async Task RemoveLine_Valid_Returns204()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        SalesOrderDetailDto created = await CreateSalesOrderAndReadAsync(client);
        await EnsureProductPriceAsync(201, "USD", 10m);
        CreateSalesOrderLineRequest secondLine = new()
        {
            ProductId = 201,
            OrderedQuantity = 3m,
            UnitPrice = 10m
        };
        await client.PostAsJsonAsync($"/api/v1/sales-orders/{created.Id}/lines", secondLine);
        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/sales-orders/{created.Id}");
        SalesOrderDetailDto? updatedSo = await getResponse.Content.ReadFromJsonAsync<SalesOrderDetailDto>();
        int lastLineId = updatedSo!.Lines[^1].Id;

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/sales-orders/{created.Id}/lines/{lastLineId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task RemoveLine_LastLine_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        SalesOrderDetailDto created = await CreateSalesOrderAndReadAsync(client);
        int onlyLineId = created.Lines[0].Id;

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/sales-orders/{created.Id}/lines/{onlyLineId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Create_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 1,
            CurrencyCode = "USD",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 100, OrderedQuantity = 10m, UnitPrice = 25m }]
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/sales-orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Create_InsufficientPermission_Returns403()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:read");
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 1,
            CurrencyCode = "USD",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 100, OrderedQuantity = 10m, UnitPrice = 25m }]
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/sales-orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>CHG-FEAT-007 §2.3 — line create without UnitPrice uses the catalog price end-to-end.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task CreateSalesOrderLine_NoUnitPrice_UsesCatalogPrice()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        await EnsureProductPriceAsync(productId: 1500, currencyCode: "USD", unitPrice: 77.77m);
        SalesOrderDetailDto so = await CreateSalesOrderAndReadAsync(client, productId: 1499, unitPrice: 10m);

        CreateSalesOrderLineRequest lineRequest = new()
        {
            ProductId = 1500,
            OrderedQuantity = 3m,
            UnitPrice = null
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/lines", lineRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SalesOrderLineDto? body = await response.Content.ReadFromJsonAsync<SalesOrderLineDto>();
        body.Should().NotBeNull();
        body!.ProductId.Should().Be(1500);
        body.UnitPrice.Should().Be(77.77m);
    }

    /// <summary>CHG-FEAT-007 §2.4 / §4 E6 — missing catalog entry blocks line creation with FULF_PRICE_NOT_FOUND.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task CreateSalesOrderLine_NoActivePrice_Returns400FulfPriceNotFound()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        SalesOrderDetailDto so = await CreateSalesOrderAndReadAsync(client, productId: 1600, unitPrice: 10m);

        // Attempt to add a line for a product with NO catalog coverage.
        CreateSalesOrderLineRequest lineRequest = new()
        {
            ProductId = 1601,
            OrderedQuantity = 2m,
            UnitPrice = null
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/lines", lineRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string raw = await response.Content.ReadAsStringAsync();
        raw.Should().Contain("FULF_PRICE_NOT_FOUND");
        raw.Should().Contain("\"productId\":1601");
        raw.Should().Contain("\"currencyCode\":\"USD\"");
    }

    /// <summary>CHG-FEAT-007 §2.3 — caller-supplied UnitPrice is preserved end-to-end.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task CreateSalesOrderLine_UnitPriceProvided_PreservesOverride()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        await EnsureProductPriceAsync(productId: 1700, currencyCode: "USD", unitPrice: 50m);
        SalesOrderDetailDto so = await CreateSalesOrderAndReadAsync(client, productId: 1699, unitPrice: 10m);

        CreateSalesOrderLineRequest lineRequest = new()
        {
            ProductId = 1700,
            OrderedQuantity = 2m,
            UnitPrice = 42m
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/lines", lineRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SalesOrderLineDto? body = await response.Content.ReadFromJsonAsync<SalesOrderLineDto>();
        body.Should().NotBeNull();
        body!.UnitPrice.Should().Be(42m, "caller override MUST be preserved (CHG-FEAT-007 §2.3).");
    }

    /// <summary>CHG-FEAT-007 §2.3 — update path is blocked when catalog loses coverage.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task UpdateSalesOrderLine_NoActivePrice_Returns400FulfPriceNotFound()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read", "sales-orders:update");
        SalesOrderDetailDto so = await CreateSalesOrderAndReadAsync(client, productId: 1800, unitPrice: 10m);
        int lineId = so.Lines[0].Id;

        // Remove the catalog entry for this product, then attempt to update the line.
        await WithDbContextAsync(async db =>
        {
            List<Warehouse.Fulfillment.DBModel.Models.ProductPrice> rows =
                await db.ProductPrices.Where(p => p.ProductId == 1800 && p.CurrencyCode == "USD").ToListAsync();
            db.ProductPrices.RemoveRange(rows);
            await db.SaveChangesAsync();
        });

        UpdateSalesOrderLineRequest update = new()
        {
            OrderedQuantity = 5m,
            UnitPrice = null
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/sales-orders/{so.Id}/lines/{lineId}", update);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string raw = await response.Content.ReadAsStringAsync();
        raw.Should().Contain("FULF_PRICE_NOT_FOUND");
    }

    /// <summary>CHG-FEAT-007 §2.9 — SO create with valid CustomerAccountId and CurrencyCode returns 201.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task PostSalesOrder_ValidCustomerAccountAndCurrency_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create", "sales-orders:read");

        // Act
        HttpResponseMessage response = await CreateSalesOrderViaApiAsync(
            client, customerAccountId: 42, currencyCode: "USD");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SalesOrderDetailDto? body = await response.Content.ReadFromJsonAsync<SalesOrderDetailDto>();
        body.Should().NotBeNull();
    }

    /// <summary>CHG-FEAT-007 §2.9 / §4 E11 — missing CustomerAccountId returns 400.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task PostSalesOrder_MissingCustomerAccountId_Returns400()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:create");
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 0,
            CurrencyCode = "USD",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 100, OrderedQuantity = 1m, UnitPrice = 10m }]
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/sales-orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
