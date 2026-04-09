using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Warehouse.Purchasing.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Tests.Integration;

/// <summary>
/// Integration tests for the PurchaseEventsController: search and filter purchase event history.
/// <para>Covers SDD-PURCH-001 purchase event audit trail operations.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
[Category("Integration")]
public sealed class PurchaseEventsControllerTests : PurchasingApiTestBase
{
    private static readonly string[] AllPermissions =
    [
        "suppliers:create", "suppliers:read",
        "purchase-orders:create", "purchase-orders:read", "purchase-orders:update",
        "purchase-events:read"
    ];

    [Test]
    public async Task Search_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Event Supplier");
        await CreatePurchaseOrderViaApiAsync(client, supplier.Id);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/purchase-events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<PurchaseEventDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<PurchaseEventDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
    }

    [Test]
    public async Task Search_ByEntityType_ReturnsFiltered()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Entity Filter Supplier");
        await CreatePurchaseOrderViaApiAsync(client, supplier.Id);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/purchase-events?EntityType=PurchaseOrder");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<PurchaseEventDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<PurchaseEventDto>>();
        body.Should().NotBeNull();
    }

    [Test]
    public async Task Search_ByEventType_ReturnsFiltered()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Event Type Filter Supplier");
        await CreatePurchaseOrderViaApiAsync(client, supplier.Id);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/purchase-events?EventType=Created");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<PurchaseEventDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<PurchaseEventDto>>();
        body.Should().NotBeNull();
    }

    [Test]
    public async Task Search_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/purchase-events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Search_InsufficientPermission_Returns403()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:read");

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/purchase-events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
