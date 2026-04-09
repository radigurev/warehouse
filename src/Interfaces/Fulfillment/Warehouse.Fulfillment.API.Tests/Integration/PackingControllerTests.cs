using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Tests.Integration;

/// <summary>
/// Integration tests for PackingController covering parcel CRUD and item management.
/// <para>Covers SDD-FULF-001 packing operations. Routes use /api/v1/sales-orders/{soId}/parcels.</para>
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
[Category("Integration")]
public sealed class PackingControllerTests : FulfillmentApiTestBase
{
    private const string AllPermissions = "sales-orders:create,sales-orders:read,sales-orders:update,pick-lists:create,pick-lists:read,pick-lists:update,packing:create,packing:read,packing:update";

    /// <summary>
    /// Creates a confirmed SO with a completed pick list and returns the SO detail plus pick list.
    /// </summary>
    private async Task<(SalesOrderDetailDto So, PickListDetailDto PickList)> CreatePickedSalesOrderAsync(HttpClient client)
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
        HttpResponseMessage refreshResponse = await client.GetAsync($"/api/v1/sales-orders/{so.Id}");
        SalesOrderDetailDto? refreshedSo = await refreshResponse.Content.ReadFromJsonAsync<SalesOrderDetailDto>();
        return (refreshedSo!, pickList);
    }

    [Test]
    public async Task CreateParcel_Valid_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions.Split(','));
        (SalesOrderDetailDto so, PickListDetailDto _) = await CreatePickedSalesOrderAsync(client);
        CreateParcelRequest request = new()
        {
            Weight = 2.5m,
            Length = 30m,
            Width = 20m,
            Height = 15m,
            Notes = "Test parcel"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/parcels", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ParcelDto? body = await response.Content.ReadFromJsonAsync<ParcelDto>();
        body.Should().NotBeNull();
        body!.SalesOrderId.Should().Be(so.Id);
        body.Weight.Should().Be(2.5m);
    }

    [Test]
    public async Task CreateParcel_NonExistentSO_Returns404()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("packing:create");
        CreateParcelRequest request = new() { Notes = "No SO" };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/sales-orders/999999/parcels", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetParcel_Existing_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions.Split(','));
        (SalesOrderDetailDto so, PickListDetailDto _) = await CreatePickedSalesOrderAsync(client);
        CreateParcelRequest createRequest = new() { Notes = "Get me" };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/parcels", createRequest);
        ParcelDto? created = await createResponse.Content.ReadFromJsonAsync<ParcelDto>();

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/sales-orders/{so.Id}/parcels/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ParcelDto? body = await response.Content.ReadFromJsonAsync<ParcelDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
    }

    [Test]
    public async Task ListParcels_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions.Split(','));
        (SalesOrderDetailDto so, PickListDetailDto _) = await CreatePickedSalesOrderAsync(client);
        CreateParcelRequest createRequest = new() { Notes = "Listed parcel" };
        await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/parcels", createRequest);

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/sales-orders/{so.Id}/parcels");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        IReadOnlyList<ParcelDto>? body = await response.Content.ReadFromJsonAsync<IReadOnlyList<ParcelDto>>();
        body.Should().NotBeNull();
        body!.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Test]
    public async Task UpdateParcel_Valid_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions.Split(','));
        (SalesOrderDetailDto so, PickListDetailDto _) = await CreatePickedSalesOrderAsync(client);
        CreateParcelRequest createRequest = new() { Weight = 1.0m };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/parcels", createRequest);
        ParcelDto? created = await createResponse.Content.ReadFromJsonAsync<ParcelDto>();
        UpdateParcelRequest updateRequest = new()
        {
            Weight = 3.0m,
            TrackingNumber = "TRK-123",
            Notes = "Updated"
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/sales-orders/{so.Id}/parcels/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ParcelDto? body = await response.Content.ReadFromJsonAsync<ParcelDto>();
        body.Should().NotBeNull();
        body!.Weight.Should().Be(3.0m);
        body.TrackingNumber.Should().Be("TRK-123");
    }

    [Test]
    public async Task DeleteParcel_Returns204()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions.Split(','));
        (SalesOrderDetailDto so, PickListDetailDto _) = await CreatePickedSalesOrderAsync(client);
        CreateParcelRequest createRequest = new() { Notes = "Doomed parcel" };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/parcels", createRequest);
        ParcelDto? created = await createResponse.Content.ReadFromJsonAsync<ParcelDto>();

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/sales-orders/{so.Id}/parcels/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task AddItem_Valid_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions.Split(','));
        (SalesOrderDetailDto so, PickListDetailDto pickList) = await CreatePickedSalesOrderAsync(client);
        CreateParcelRequest createRequest = new() { Notes = "For item test" };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/parcels", createRequest);
        ParcelDto? parcel = await createResponse.Content.ReadFromJsonAsync<ParcelDto>();
        AddParcelItemRequest itemRequest = new()
        {
            ProductId = so.Lines[0].ProductId,
            Quantity = 5m,
            PickListLineId = pickList.Lines[0].Id
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/parcels/{parcel!.Id}/items", itemRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ParcelItemDto? body = await response.Content.ReadFromJsonAsync<ParcelItemDto>();
        body.Should().NotBeNull();
        body!.ProductId.Should().Be(so.Lines[0].ProductId);
        body.Quantity.Should().Be(5m);
    }

    [Test]
    public async Task RemoveItem_Returns204()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions.Split(','));
        (SalesOrderDetailDto so, PickListDetailDto pickList) = await CreatePickedSalesOrderAsync(client);
        CreateParcelRequest createRequest = new() { Notes = "Item removal test" };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/parcels", createRequest);
        ParcelDto? parcel = await createResponse.Content.ReadFromJsonAsync<ParcelDto>();
        AddParcelItemRequest itemRequest = new()
        {
            ProductId = so.Lines[0].ProductId,
            Quantity = 3m,
            PickListLineId = pickList.Lines[0].Id
        };
        HttpResponseMessage addResponse = await client.PostAsJsonAsync($"/api/v1/sales-orders/{so.Id}/parcels/{parcel!.Id}/items", itemRequest);
        ParcelItemDto? item = await addResponse.Content.ReadFromJsonAsync<ParcelItemDto>();

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/sales-orders/{so.Id}/parcels/{parcel.Id}/items/{item!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task CreateParcel_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        CreateParcelRequest request = new() { Notes = "Unauth" };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/sales-orders/1/parcels", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateParcel_InsufficientPermission_Returns403()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("packing:read");
        CreateParcelRequest request = new() { Notes = "No perm" };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/sales-orders/1/parcels", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
