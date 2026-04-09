using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Tests.Integration;

/// <summary>
/// Integration tests for CarriersController covering carrier CRUD, deactivation, and service level management.
/// <para>Covers SDD-FULF-001 carrier management sections.</para>
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
[Category("Integration")]
public sealed class CarriersControllerTests : FulfillmentApiTestBase
{
    [Test]
    public async Task Create_Valid_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("carriers:create", "carriers:read");

        // Act
        HttpResponseMessage response = await CreateCarrierViaApiAsync(client, code: "DHL-001", name: "DHL Express");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CarrierDetailDto? body = await response.Content.ReadFromJsonAsync<CarrierDetailDto>();
        body.Should().NotBeNull();
        body!.Code.Should().Be("DHL-001");
        body.Name.Should().Be("DHL Express");
        body.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task Create_DuplicateCode_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("carriers:create", "carriers:read");
        await CreateCarrierAndReadAsync(client, code: "DUP-CAR");

        // Act
        HttpResponseMessage response = await CreateCarrierViaApiAsync(client, code: "DUP-CAR", name: "Another Carrier");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Get_Existing_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("carriers:create", "carriers:read");
        CarrierDto created = await CreateCarrierAndReadAsync(client);

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/carriers/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CarrierDetailDto? body = await response.Content.ReadFromJsonAsync<CarrierDetailDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
    }

    [Test]
    public async Task Get_NonExistent_Returns404()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("carriers:read");

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/carriers/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Search_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("carriers:create", "carriers:read");
        await CreateCarrierAndReadAsync(client, name: "FedEx");
        await CreateCarrierAndReadAsync(client, name: "UPS");

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/carriers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<CarrierDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<CarrierDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Test]
    public async Task Update_Valid_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("carriers:create", "carriers:read", "carriers:update");
        CarrierDto created = await CreateCarrierAndReadAsync(client, name: "Old Name");
        UpdateCarrierRequest request = new()
        {
            Name = "New Carrier Name",
            IsActive = true,
            ContactPhone = "+1-555-0100"
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/carriers/{created.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CarrierDetailDto? body = await response.Content.ReadFromJsonAsync<CarrierDetailDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("New Carrier Name");
    }

    [Test]
    public async Task Deactivate_Active_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("carriers:create", "carriers:read", "carriers:update");
        CarrierDto created = await CreateCarrierAndReadAsync(client);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/carriers/{created.Id}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CarrierDetailDto? body = await response.Content.ReadFromJsonAsync<CarrierDetailDto>();
        body.Should().NotBeNull();
        body!.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task CreateServiceLevel_Valid_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("carriers:create", "carriers:read", "carriers:update");
        CarrierDto carrier = await CreateCarrierAndReadAsync(client);
        CreateCarrierServiceLevelRequest request = new()
        {
            Code = "EXPRESS",
            Name = "Express Delivery",
            EstimatedDeliveryDays = 2,
            BaseRate = 15.00m
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/carriers/{carrier.Id}/service-levels", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CarrierServiceLevelDto? body = await response.Content.ReadFromJsonAsync<CarrierServiceLevelDto>();
        body.Should().NotBeNull();
        body!.Code.Should().Be("EXPRESS");
        body.Name.Should().Be("Express Delivery");
    }

    [Test]
    public async Task CreateServiceLevel_DuplicateCode_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("carriers:create", "carriers:read", "carriers:update");
        CarrierDto carrier = await CreateCarrierAndReadAsync(client);
        CreateCarrierServiceLevelRequest request = new()
        {
            Code = "DUP-SL",
            Name = "Standard"
        };
        await client.PostAsJsonAsync($"/api/v1/carriers/{carrier.Id}/service-levels", request);

        CreateCarrierServiceLevelRequest duplicate = new()
        {
            Code = "DUP-SL",
            Name = "Another Standard"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/carriers/{carrier.Id}/service-levels", duplicate);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task ListServiceLevels_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("carriers:create", "carriers:read", "carriers:update");
        CarrierDto carrier = await CreateCarrierAndReadAsync(client);
        CreateCarrierServiceLevelRequest request = new()
        {
            Code = "STD",
            Name = "Standard"
        };
        await client.PostAsJsonAsync($"/api/v1/carriers/{carrier.Id}/service-levels", request);

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/carriers/{carrier.Id}/service-levels");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        IReadOnlyList<CarrierServiceLevelDto>? body = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<CarrierServiceLevelDto>>();
        body.Should().NotBeNull();
        body!.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Test]
    public async Task UpdateServiceLevel_Valid_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("carriers:create", "carriers:read", "carriers:update");
        CarrierDto carrier = await CreateCarrierAndReadAsync(client);
        CreateCarrierServiceLevelRequest createRequest = new()
        {
            Code = "UPD-SL",
            Name = "Original Name"
        };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync($"/api/v1/carriers/{carrier.Id}/service-levels", createRequest);
        CarrierServiceLevelDto? createdLevel = await createResponse.Content.ReadFromJsonAsync<CarrierServiceLevelDto>();
        UpdateCarrierServiceLevelRequest updateRequest = new()
        {
            Name = "Updated Name",
            EstimatedDeliveryDays = 5,
            BaseRate = 20.00m
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/carriers/{carrier.Id}/service-levels/{createdLevel!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CarrierServiceLevelDto? body = await response.Content.ReadFromJsonAsync<CarrierServiceLevelDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Updated Name");
    }

    [Test]
    public async Task DeleteServiceLevel_Returns204()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("carriers:create", "carriers:read", "carriers:update");
        CarrierDto carrier = await CreateCarrierAndReadAsync(client);
        CreateCarrierServiceLevelRequest createRequest = new()
        {
            Code = "DEL-SL",
            Name = "To Delete"
        };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync($"/api/v1/carriers/{carrier.Id}/service-levels", createRequest);
        CarrierServiceLevelDto? createdLevel = await createResponse.Content.ReadFromJsonAsync<CarrierServiceLevelDto>();

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/carriers/{carrier.Id}/service-levels/{createdLevel!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Create_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        CreateCarrierRequest request = new()
        {
            Code = "UNAUTH",
            Name = "Unauth Carrier"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/carriers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Create_InsufficientPermission_Returns403()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("carriers:read");
        CreateCarrierRequest request = new()
        {
            Code = "NOPERM",
            Name = "No Perm Carrier"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/carriers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
