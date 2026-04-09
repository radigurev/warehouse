using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Tests.Integration;

/// <summary>
/// Integration tests for ShipmentsController covering creation, search, status updates, and tracking.
/// <para>Covers SDD-FULF-001 shipment dispatch and tracking operations.</para>
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
[Category("Integration")]
public sealed class ShipmentsControllerTests : FulfillmentApiTestBase
{
    private static readonly string[] FullPermissions =
    [
        "sales-orders:create", "sales-orders:read", "sales-orders:update",
        "pick-lists:create", "pick-lists:read", "pick-lists:update",
        "packing:create", "packing:read", "packing:update",
        "shipments:create", "shipments:read", "shipments:update"
    ];

    /// <summary>
    /// Creates a confirmed SO with completed picking and at least one packed parcel, then creates a shipment.
    /// </summary>
    private async Task<(SalesOrderDetailDto So, ShipmentDetailDto Shipment)> CreateShipmentFlowAsync(HttpClient client)
    {
        SalesOrderDetailDto so = await CreateSalesOrderAndReadAsync(client);
        await client.PostAsync($"/api/v1/sales-orders/{so.Id}/confirm", null);

        GeneratePickListRequest genRequest = new() { SalesOrderId = so.Id };
        HttpResponseMessage plResponse = await client.PostAsJsonAsync("/api/v1/pick-lists", genRequest);
        PickListDetailDto? pickList = await plResponse.Content.ReadFromJsonAsync<PickListDetailDto>();
        foreach (PickListLineDto line in pickList!.Lines)
        {
            ConfirmPickRequest pickRequest = new() { ActualQuantity = line.RequestedQuantity };
            await client.PostAsJsonAsync($"/api/v1/pick-lists/{pickList.Id}/lines/{line.Id}/pick", pickRequest);
        }

        CreateParcelRequest parcelRequest = new() { Weight = 1.0m };
        HttpResponseMessage parcelResponse = await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/parcels", parcelRequest);
        ParcelDto? parcel = await parcelResponse.Content.ReadFromJsonAsync<ParcelDto>();
        AddParcelItemRequest itemRequest = new()
        {
            ProductId = so.Lines[0].ProductId,
            Quantity = so.Lines[0].OrderedQuantity,
            PickListLineId = pickList.Lines[0].Id
        };
        await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/parcels/{parcel!.Id}/items", itemRequest);

        CreateShipmentRequest shipRequest = new() { SalesOrderId = so.Id };
        HttpResponseMessage shipResponse = await client.PostAsJsonAsync("/api/v1/shipments", shipRequest);
        ShipmentDetailDto? shipment = await shipResponse.Content.ReadFromJsonAsync<ShipmentDetailDto>();
        return (so, shipment!);
    }

    [Test]
    public async Task Create_Valid_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(FullPermissions);
        SalesOrderDetailDto so = await CreateSalesOrderAndReadAsync(client);
        await client.PostAsync($"/api/v1/sales-orders/{so.Id}/confirm", null);
        GeneratePickListRequest genRequest = new() { SalesOrderId = so.Id };
        HttpResponseMessage plResponse = await client.PostAsJsonAsync("/api/v1/pick-lists", genRequest);
        PickListDetailDto? pickList = await plResponse.Content.ReadFromJsonAsync<PickListDetailDto>();
        foreach (PickListLineDto line in pickList!.Lines)
        {
            ConfirmPickRequest pickRequest = new() { ActualQuantity = line.RequestedQuantity };
            await client.PostAsJsonAsync($"/api/v1/pick-lists/{pickList.Id}/lines/{line.Id}/pick", pickRequest);
        }
        CreateParcelRequest parcelRequest = new() { Weight = 1.0m };
        HttpResponseMessage parcelResponse = await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/parcels", parcelRequest);
        ParcelDto? parcel = await parcelResponse.Content.ReadFromJsonAsync<ParcelDto>();
        AddParcelItemRequest itemRequest = new()
        {
            ProductId = so.Lines[0].ProductId,
            Quantity = so.Lines[0].OrderedQuantity,
            PickListLineId = pickList.Lines[0].Id
        };
        await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/parcels/{parcel!.Id}/items", itemRequest);
        CreateShipmentRequest shipRequest = new() { SalesOrderId = so.Id };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/shipments", shipRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ShipmentDetailDto? body = await response.Content.ReadFromJsonAsync<ShipmentDetailDto>();
        body.Should().NotBeNull();
        body!.SalesOrderId.Should().Be(so.Id);
        body.Status.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Create_NonExistentSO_Returns404()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("shipments:create");
        CreateShipmentRequest request = new() { SalesOrderId = 999999 };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/shipments", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Get_Existing_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(FullPermissions);
        (SalesOrderDetailDto _, ShipmentDetailDto shipment) = await CreateShipmentFlowAsync(client);

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/shipments/{shipment.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ShipmentDetailDto? body = await response.Content.ReadFromJsonAsync<ShipmentDetailDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(shipment.Id);
    }

    [Test]
    public async Task Search_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(FullPermissions);
        await CreateShipmentFlowAsync(client);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/shipments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<ShipmentDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<ShipmentDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Test]
    public async Task UpdateStatus_Valid_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(FullPermissions);
        (SalesOrderDetailDto _, ShipmentDetailDto shipment) = await CreateShipmentFlowAsync(client);
        UpdateShipmentStatusRequest statusRequest = new()
        {
            Status = "InTransit",
            TrackingNumber = "TRK-456",
            Notes = "Package picked up"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/status", statusRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ShipmentDetailDto? body = await response.Content.ReadFromJsonAsync<ShipmentDetailDto>();
        body.Should().NotBeNull();
    }

    [Test]
    public async Task GetTracking_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(FullPermissions);
        (SalesOrderDetailDto _, ShipmentDetailDto shipment) = await CreateShipmentFlowAsync(client);

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/shipments/{shipment.Id}/tracking");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        IReadOnlyList<ShipmentTrackingDto>? body = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<ShipmentTrackingDto>>();
        body.Should().NotBeNull();
    }

    [Test]
    public async Task Create_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        CreateShipmentRequest request = new() { SalesOrderId = 1 };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/shipments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Create_InsufficientPermission_Returns403()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("shipments:read");
        CreateShipmentRequest request = new() { SalesOrderId = 1 };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/shipments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
