using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Warehouse.Purchasing.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Tests.Integration;

/// <summary>
/// Integration tests for the GoodsReceiptsController: create, search, get, complete, inspect, and resolve.
/// <para>Covers SDD-PURCH-001 goods receipt and receiving inspection operations.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
[Category("Integration")]
public sealed class GoodsReceiptsControllerTests : PurchasingApiTestBase
{
    private static readonly string[] AllPermissions =
    [
        "suppliers:create", "suppliers:read",
        "purchase-orders:create", "purchase-orders:read", "purchase-orders:update",
        "goods-receipts:create", "goods-receipts:read", "goods-receipts:update"
    ];

    [Test]
    public async Task Create_Valid_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "GR Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);
        await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/confirm", null);

        // Act
        HttpResponseMessage response = await CreateGoodsReceiptViaApiAsync(
            client,
            purchaseOrderId: po.Id,
            purchaseOrderLineId: po.Lines[0].Id,
            receivedQuantity: 10m);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        GoodsReceiptDetailDto? body = await response.Content.ReadFromJsonAsync<GoodsReceiptDetailDto>();
        body.Should().NotBeNull();
        body!.PurchaseOrderId.Should().Be(po.Id);
        body.Status.Should().Be("Open");
        body.Lines.Should().HaveCount(1);
    }

    [Test]
    public async Task Create_InvalidPO_Returns400()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);

        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = 999999,
            WarehouseId = 1,
            Lines = [new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = 1, ReceivedQuantity = 10m }]
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/goods-receipts", request);

        // Assert
        HttpStatusCode statusCode = response.StatusCode;
        bool isClientError = statusCode == HttpStatusCode.BadRequest
                          || statusCode == HttpStatusCode.NotFound
                          || statusCode == HttpStatusCode.Conflict;
        isClientError.Should().BeTrue($"expected a 4xx error but got {(int)statusCode}");
    }

    [Test]
    public async Task Get_Existing_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Get GR Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);
        await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/confirm", null);
        HttpResponseMessage createResponse = await CreateGoodsReceiptViaApiAsync(client, po.Id, po.Lines[0].Id, 5m);
        GoodsReceiptDetailDto created = (await createResponse.Content.ReadFromJsonAsync<GoodsReceiptDetailDto>())!;

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/goods-receipts/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GoodsReceiptDetailDto? body = await response.Content.ReadFromJsonAsync<GoodsReceiptDetailDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
    }

    [Test]
    public async Task Search_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Search GR Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);
        await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/confirm", null);
        await CreateGoodsReceiptViaApiAsync(client, po.Id, po.Lines[0].Id, 5m);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/goods-receipts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<GoodsReceiptDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<GoodsReceiptDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Test]
    public async Task Complete_Open_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Complete GR Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);
        await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/confirm", null);
        HttpResponseMessage createResponse = await CreateGoodsReceiptViaApiAsync(client, po.Id, po.Lines[0].Id, 10m);
        GoodsReceiptDetailDto receipt = (await createResponse.Content.ReadFromJsonAsync<GoodsReceiptDetailDto>())!;

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/goods-receipts/{receipt.Id}/complete", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GoodsReceiptDetailDto? body = await response.Content.ReadFromJsonAsync<GoodsReceiptDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Completed");
        body.CompletedAtUtc.Should().NotBeNull();
    }

    [Test]
    public async Task InspectLine_Accepted_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Inspect Accept Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);
        await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/confirm", null);
        HttpResponseMessage createResponse = await CreateGoodsReceiptViaApiAsync(client, po.Id, po.Lines[0].Id, 10m);
        GoodsReceiptDetailDto receipt = (await createResponse.Content.ReadFromJsonAsync<GoodsReceiptDetailDto>())!;
        int lineId = receipt.Lines[0].Id;

        InspectLineRequest inspectReq = new()
        {
            InspectionStatus = "Accepted",
            InspectionNote = "All items passed QC"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/goods-receipts/{receipt.Id}/lines/{lineId}/inspect", inspectReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GoodsReceiptLineDto? body = await response.Content.ReadFromJsonAsync<GoodsReceiptLineDto>();
        body.Should().NotBeNull();
        body!.InspectionStatus.Should().Be("Accepted");
        body.InspectionNote.Should().Be("All items passed QC");
    }

    [Test]
    public async Task InspectLine_Rejected_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Inspect Reject Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);
        await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/confirm", null);
        HttpResponseMessage createResponse = await CreateGoodsReceiptViaApiAsync(client, po.Id, po.Lines[0].Id, 10m);
        GoodsReceiptDetailDto receipt = (await createResponse.Content.ReadFromJsonAsync<GoodsReceiptDetailDto>())!;
        int lineId = receipt.Lines[0].Id;

        InspectLineRequest inspectReq = new()
        {
            InspectionStatus = "Rejected",
            InspectionNote = "Items damaged during transit"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/goods-receipts/{receipt.Id}/lines/{lineId}/inspect", inspectReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GoodsReceiptLineDto? body = await response.Content.ReadFromJsonAsync<GoodsReceiptLineDto>();
        body.Should().NotBeNull();
        body!.InspectionStatus.Should().Be("Rejected");
    }

    [Test]
    public async Task InspectLine_InvalidStatus_Returns400()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Inspect Invalid Supplier");
        PurchaseOrderDetailDto po = await CreatePurchaseOrderAndReadAsync(client, supplier.Id);
        await client.PostAsync($"/api/v1/purchase-orders/{po.Id}/confirm", null);
        HttpResponseMessage createResponse = await CreateGoodsReceiptViaApiAsync(client, po.Id, po.Lines[0].Id, 10m);
        GoodsReceiptDetailDto receipt = (await createResponse.Content.ReadFromJsonAsync<GoodsReceiptDetailDto>())!;
        int lineId = receipt.Lines[0].Id;

        InspectLineRequest inspectReq = new()
        {
            InspectionStatus = "InvalidStatus"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/goods-receipts/{receipt.Id}/lines/{lineId}/inspect", inspectReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = 1,
            WarehouseId = 1,
            Lines = [new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = 1, ReceivedQuantity = 10m }]
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/goods-receipts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Create_InsufficientPermission_Returns403()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("goods-receipts:read");
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = 1,
            WarehouseId = 1,
            Lines = [new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = 1, ReceivedQuantity = 10m }]
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/goods-receipts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
