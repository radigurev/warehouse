using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Purchasing.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Tests.Integration;

/// <summary>
/// Integration tests for the PurchaseOrdersController: create, search, get, update,
/// confirm, cancel, close, and line management.
/// <para>Covers SDD-PURCH-001 purchase order lifecycle operations.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
[Category("Integration")]
public sealed class PurchaseOrdersControllerTests : PurchasingApiTestBase
{
    [Test]
    public async Task Create_Valid_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "purchase-orders:create", "purchase-orders:read");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "PO Supplier");

        // Act
        HttpResponseMessage response = await CreatePurchaseOrderViaApiAsync(client, supplier.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        PurchaseOrderDetailDto? body = await response.Content.ReadFromJsonAsync<PurchaseOrderDetailDto>();
        body.Should().NotBeNull();
        body!.SupplierId.Should().Be(supplier.Id);
        body.Status.Should().Be("Draft");
        body.Lines.Should().HaveCount(1);
        body.TotalAmount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task Create_EmptyLines_Returns400()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "purchase-orders:create");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Empty Lines Supplier");

        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = supplier.Id,
            DestinationWarehouseId = 1,
            Lines = []
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/purchase-orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Get_Existing_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "purchase-orders:create", "purchase-orders:read");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Get PO Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/purchase-orders/{po.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PurchaseOrderDetailDto? body = await response.Content.ReadFromJsonAsync<PurchaseOrderDetailDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(po.Id);
        body.Lines.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Test]
    public async Task Get_NonExistent_Returns404()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("purchase-orders:read");

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/purchase-orders/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Search_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "purchase-orders:create", "purchase-orders:read");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Search PO Supplier");
        await CreatePurchaseOrderViaApiAsync(client, supplier.Id);
        await CreatePurchaseOrderViaApiAsync(client, supplier.Id);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/purchase-orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<PurchaseOrderDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<PurchaseOrderDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Test]
    public async Task Update_Valid_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "purchase-orders:create", "purchase-orders:read", "purchase-orders:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Update PO Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);

        UpdatePurchaseOrderRequest request = new()
        {
            DestinationWarehouseId = 1,
            Notes = "Updated PO notes"
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/purchase-orders/{po.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PurchaseOrderDetailDto? body = await response.Content.ReadFromJsonAsync<PurchaseOrderDetailDto>();
        body.Should().NotBeNull();
        body!.Notes.Should().Be("Updated PO notes");
    }

    [Test]
    public async Task Confirm_Draft_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "purchase-orders:create", "purchase-orders:read", "purchase-orders:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Confirm PO Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/confirm", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PurchaseOrderDetailDto? body = await response.Content.ReadFromJsonAsync<PurchaseOrderDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Confirmed");
        body.ConfirmedAtUtc.Should().NotBeNull();
    }

    [Test]
    public async Task Confirm_NonDraft_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "purchase-orders:create", "purchase-orders:read", "purchase-orders:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Double Confirm Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);
        await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/confirm", null);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/confirm", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Cancel_Draft_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "purchase-orders:create", "purchase-orders:read", "purchase-orders:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Cancel PO Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PurchaseOrderDetailDto? body = await response.Content.ReadFromJsonAsync<PurchaseOrderDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Cancelled");
    }

    [Test]
    public async Task Cancel_NonDraft_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(
            "suppliers:create", "suppliers:read",
            "purchase-orders:create", "purchase-orders:read", "purchase-orders:update",
            "goods-receipts:create", "goods-receipts:read", "goods-receipts:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Cancel Received Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);
        await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/confirm", null);

        HttpResponseMessage grResponse = await CreateGoodsReceiptViaApiAsync(client, po.Id, po.Lines[0].Id, 10m);
        GoodsReceiptDetailDto receipt = (await grResponse.Content.ReadFromJsonAsync<GoodsReceiptDetailDto>())!;
        int grLineId = receipt.Lines[0].Id;

        InspectLineRequest inspectReq = new() { InspectionStatus = "Accepted" };
        await client.PostAsJsonAsync($"/api/v1/goods-receipts/{receipt.Id}/lines/{grLineId}/inspect", inspectReq);
        await client.PostAsync($"/api/v1/goods-receipts/{receipt.Id}/complete", null);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Close_Confirmed_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(
            "suppliers:create", "suppliers:read",
            "purchase-orders:create", "purchase-orders:read", "purchase-orders:update",
            "goods-receipts:create", "goods-receipts:read", "goods-receipts:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Close PO Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);
        await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/confirm", null);

        HttpResponseMessage grResponse = await CreateGoodsReceiptViaApiAsync(client, po.Id, po.Lines[0].Id, 50m);
        GoodsReceiptDetailDto receipt = (await grResponse.Content.ReadFromJsonAsync<GoodsReceiptDetailDto>())!;
        int grLineId = receipt.Lines[0].Id;

        InspectLineRequest inspectReq = new() { InspectionStatus = "Accepted" };
        await client.PostAsJsonAsync($"/api/v1/goods-receipts/{receipt.Id}/lines/{grLineId}/inspect", inspectReq);
        await client.PostAsync($"/api/v1/goods-receipts/{receipt.Id}/complete", null);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/close", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PurchaseOrderDetailDto? body = await response.Content.ReadFromJsonAsync<PurchaseOrderDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Closed");
        body.ClosedAtUtc.Should().NotBeNull();
    }

    [Test]
    public async Task AddLine_Valid_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "purchase-orders:create", "purchase-orders:read", "purchase-orders:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Add Line Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);

        CreatePurchaseOrderLineRequest lineReq = new()
        {
            ProductId = 200,
            OrderedQuantity = 25m,
            UnitPrice = 15m
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/purchase-orders/{po.Id}/lines", lineReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        PurchaseOrderLineDto? body = await response.Content.ReadFromJsonAsync<PurchaseOrderLineDto>();
        body.Should().NotBeNull();
        body!.ProductId.Should().Be(200);
        body.OrderedQuantity.Should().Be(25m);
        body.UnitPrice.Should().Be(15m);
    }

    [Test]
    public async Task UpdateLine_Valid_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "purchase-orders:create", "purchase-orders:read", "purchase-orders:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Update Line Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);
        int lineId = po.Lines[0].Id;

        UpdatePurchaseOrderLineRequest updateReq = new()
        {
            OrderedQuantity = 100m,
            UnitPrice = 20m,
            Notes = "Updated line notes"
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/purchase-orders/{po.Id}/lines/{lineId}", updateReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PurchaseOrderLineDto? body = await response.Content.ReadFromJsonAsync<PurchaseOrderLineDto>();
        body.Should().NotBeNull();
        body!.OrderedQuantity.Should().Be(100m);
        body.UnitPrice.Should().Be(20m);
    }

    [Test]
    public async Task RemoveLine_Valid_Returns204()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "purchase-orders:create", "purchase-orders:read", "purchase-orders:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Remove Line Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);

        CreatePurchaseOrderLineRequest lineReq = new()
        {
            ProductId = 300,
            OrderedQuantity = 10m,
            UnitPrice = 5m
        };
        HttpResponseMessage addLineResponse = await client.PostAsJsonAsync($"/api/v1/purchase-orders/{po.Id}/lines", lineReq);
        PurchaseOrderLineDto addedLine = (await addLineResponse.Content.ReadFromJsonAsync<PurchaseOrderLineDto>())!;

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/purchase-orders/{po.Id}/lines/{addedLine.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Create_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = 1,
            DestinationWarehouseId = 1,
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = 1, OrderedQuantity = 1, UnitPrice = 1 }]
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/purchase-orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Create_InsufficientPermission_Returns403()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("purchase-orders:read");
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = 1,
            DestinationWarehouseId = 1,
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = 1, OrderedQuantity = 1, UnitPrice = 1 }]
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/purchase-orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
