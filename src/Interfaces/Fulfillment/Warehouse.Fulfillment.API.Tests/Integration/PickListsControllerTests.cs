using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Tests.Integration;

/// <summary>
/// Integration tests for PickListsController covering generation, search, pick confirmation, and cancellation.
/// <para>Covers SDD-FULF-001 pick list operations.</para>
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
[Category("Integration")]
public sealed class PickListsControllerTests : FulfillmentApiTestBase
{
    [Test]
    public async Task Generate_Valid_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(
            "sales-orders:create", "sales-orders:read", "sales-orders:update",
            "pick-lists:create", "pick-lists:read");
        SalesOrderDetailDto so = await CreateSalesOrderAndReadAsync(client);
        await client.PostAsync($"/api/v1/sales-orders/{so.Id}/confirm", null);
        GeneratePickListRequest request = new() { SalesOrderId = so.Id };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/pick-lists", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        PickListDetailDto? body = await response.Content.ReadFromJsonAsync<PickListDetailDto>();
        body.Should().NotBeNull();
        body!.SalesOrderId.Should().Be(so.Id);
        body.Status.Should().Be("Pending");
        body.Lines.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Test]
    public async Task Generate_NonConfirmedSO_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(
            "sales-orders:create", "sales-orders:read",
            "pick-lists:create");
        SalesOrderDetailDto so = await CreateSalesOrderAndReadAsync(client);
        GeneratePickListRequest request = new() { SalesOrderId = so.Id };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/pick-lists", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Get_Existing_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(
            "sales-orders:create", "sales-orders:read", "sales-orders:update",
            "pick-lists:create", "pick-lists:read");
        SalesOrderDetailDto so = await CreateSalesOrderAndReadAsync(client);
        await client.PostAsync($"/api/v1/sales-orders/{so.Id}/confirm", null);
        GeneratePickListRequest request = new() { SalesOrderId = so.Id };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/v1/pick-lists", request);
        PickListDetailDto? created = await createResponse.Content.ReadFromJsonAsync<PickListDetailDto>();

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/pick-lists/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PickListDetailDto? body = await response.Content.ReadFromJsonAsync<PickListDetailDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
    }

    [Test]
    public async Task Search_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(
            "sales-orders:create", "sales-orders:read", "sales-orders:update",
            "pick-lists:create", "pick-lists:read");
        SalesOrderDetailDto so = await CreateSalesOrderAndReadAsync(client);
        await client.PostAsync($"/api/v1/sales-orders/{so.Id}/confirm", null);
        GeneratePickListRequest request = new() { SalesOrderId = so.Id };
        await client.PostAsJsonAsync("/api/v1/pick-lists", request);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/pick-lists");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<PickListDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<PickListDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Test]
    public async Task ConfirmPick_Valid_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(
            "sales-orders:create", "sales-orders:read", "sales-orders:update",
            "pick-lists:create", "pick-lists:read", "pick-lists:update");
        SalesOrderDetailDto so = await CreateSalesOrderAndReadAsync(client);
        await client.PostAsync($"/api/v1/sales-orders/{so.Id}/confirm", null);
        GeneratePickListRequest genRequest = new() { SalesOrderId = so.Id };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/v1/pick-lists", genRequest);
        PickListDetailDto? pickList = await createResponse.Content.ReadFromJsonAsync<PickListDetailDto>();
        int lineId = pickList!.Lines[0].Id;
        ConfirmPickRequest pickRequest = new() { ActualQuantity = 10m };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/pick-lists/{pickList.Id}/lines/{lineId}/pick", pickRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PickListLineDto? body = await response.Content.ReadFromJsonAsync<PickListLineDto>();
        body.Should().NotBeNull();
        body!.ActualQuantity.Should().Be(10m);
        body.Status.Should().Be("Completed");
    }

    [Test]
    public async Task ConfirmPick_NonExistent_Returns404()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("pick-lists:update");
        ConfirmPickRequest pickRequest = new() { ActualQuantity = 10m };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/pick-lists/999999/lines/999999/pick", pickRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Cancel_Pending_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(
            "sales-orders:create", "sales-orders:read", "sales-orders:update",
            "pick-lists:create", "pick-lists:read", "pick-lists:update");
        SalesOrderDetailDto so = await CreateSalesOrderAndReadAsync(client);
        await client.PostAsync($"/api/v1/sales-orders/{so.Id}/confirm", null);
        GeneratePickListRequest genRequest = new() { SalesOrderId = so.Id };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/v1/pick-lists", genRequest);
        PickListDetailDto? pickList = await createResponse.Content.ReadFromJsonAsync<PickListDetailDto>();

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/pick-lists/{pickList!.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PickListDetailDto? body = await response.Content.ReadFromJsonAsync<PickListDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Cancelled");
    }

    [Test]
    public async Task Cancel_NonPending_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(
            "sales-orders:create", "sales-orders:read", "sales-orders:update",
            "pick-lists:create", "pick-lists:read", "pick-lists:update");
        SalesOrderDetailDto so = await CreateSalesOrderAndReadAsync(client);
        await client.PostAsync($"/api/v1/sales-orders/{so.Id}/confirm", null);
        GeneratePickListRequest genRequest = new() { SalesOrderId = so.Id };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/v1/pick-lists", genRequest);
        PickListDetailDto? pickList = await createResponse.Content.ReadFromJsonAsync<PickListDetailDto>();
        foreach (PickListLineDto line in pickList!.Lines)
        {
            ConfirmPickRequest pickRequest = new() { ActualQuantity = line.RequestedQuantity };
            await client.PostAsJsonAsync($"/api/v1/pick-lists/{pickList.Id}/lines/{line.Id}/pick", pickRequest);
        }

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/pick-lists/{pickList.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Generate_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        GeneratePickListRequest request = new() { SalesOrderId = 1 };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/pick-lists", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Generate_InsufficientPermission_Returns403()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("pick-lists:read");
        GeneratePickListRequest request = new() { SalesOrderId = 1 };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/pick-lists", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
