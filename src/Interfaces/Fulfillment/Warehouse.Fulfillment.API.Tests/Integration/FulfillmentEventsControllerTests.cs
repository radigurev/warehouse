using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Tests.Integration;

/// <summary>
/// Integration tests for FulfillmentEventsController covering event search and filtering.
/// <para>Covers SDD-FULF-001 immutable fulfillment event audit trail.</para>
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
[Category("Integration")]
public sealed class FulfillmentEventsControllerTests : FulfillmentApiTestBase
{
    [Test]
    public async Task Search_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(
            "sales-orders:create", "sales-orders:read",
            "fulfillment-events:read");
        await CreateSalesOrderAndReadAsync(client);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/fulfillment-events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<FulfillmentEventDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<FulfillmentEventDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
    }

    [Test]
    public async Task Search_ByEntityType_ReturnsFiltered()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(
            "sales-orders:create", "sales-orders:read",
            "fulfillment-events:read");
        await CreateSalesOrderAndReadAsync(client);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/fulfillment-events?EntityType=SalesOrder");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<FulfillmentEventDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<FulfillmentEventDto>>();
        body.Should().NotBeNull();
        if (body!.Items.Count > 0)
            body.Items.Should().OnlyContain(e => e.EntityType == "SalesOrder");
    }

    [Test]
    public async Task Search_ByEventType_ReturnsFiltered()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(
            "sales-orders:create", "sales-orders:read",
            "fulfillment-events:read");
        await CreateSalesOrderAndReadAsync(client);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/fulfillment-events?EventType=Created");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<FulfillmentEventDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<FulfillmentEventDto>>();
        body.Should().NotBeNull();
        if (body!.Items.Count > 0)
            body.Items.Should().OnlyContain(e => e.EventType == "Created");
    }

    [Test]
    public async Task Search_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/fulfillment-events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Search_InsufficientPermission_Returns403()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("sales-orders:read");

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/fulfillment-events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
