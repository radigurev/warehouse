using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Tests.Integration;

/// <summary>
/// Integration tests for CustomerReturnsController covering RMA lifecycle operations.
/// <para>Covers SDD-FULF-001 customer return management: Draft -> Confirmed -> Received -> Closed, with cancellation.</para>
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
[Category("Integration")]
public sealed class CustomerReturnsControllerTests : FulfillmentApiTestBase
{
    /// <summary>
    /// Creates a customer return via the API and reads the detail DTO.
    /// </summary>
    private async Task<CustomerReturnDetailDto> CreateReturnAndReadAsync(HttpClient client)
    {
        HttpResponseMessage response = await CreateCustomerReturnViaApiAsync(client);
        response.EnsureSuccessStatusCode();
        CustomerReturnDetailDto? dto = await response.Content.ReadFromJsonAsync<CustomerReturnDetailDto>();
        return dto!;
    }

    [Test]
    public async Task Create_Valid_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customer-returns:create", "customer-returns:read");

        // Act
        HttpResponseMessage response = await CreateCustomerReturnViaApiAsync(client);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CustomerReturnDetailDto? body = await response.Content.ReadFromJsonAsync<CustomerReturnDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Draft");
        body.Reason.Should().Be("Damaged in transit");
        body.Lines.Should().HaveCount(1);
    }

    [Test]
    public async Task Create_EmptyLines_Returns400()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customer-returns:create");
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = 1,
            Reason = "Missing items",
            Lines = []
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/customer-returns", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Get_Existing_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customer-returns:create", "customer-returns:read");
        CustomerReturnDetailDto created = await CreateReturnAndReadAsync(client);

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/customer-returns/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CustomerReturnDetailDto? body = await response.Content.ReadFromJsonAsync<CustomerReturnDetailDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
        body.ReturnNumber.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Search_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customer-returns:create", "customer-returns:read");
        await CreateReturnAndReadAsync(client);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/customer-returns");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<CustomerReturnDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<CustomerReturnDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Test]
    public async Task Confirm_Draft_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customer-returns:create", "customer-returns:read", "customer-returns:update");
        CustomerReturnDetailDto created = await CreateReturnAndReadAsync(client);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/customer-returns/{created.Id}/confirm", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CustomerReturnDetailDto? body = await response.Content.ReadFromJsonAsync<CustomerReturnDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Confirmed");
        body.ConfirmedAtUtc.Should().NotBeNull();
    }

    [Test]
    public async Task Confirm_NonDraft_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customer-returns:create", "customer-returns:read", "customer-returns:update");
        CustomerReturnDetailDto created = await CreateReturnAndReadAsync(client);
        await client.PostAsync($"/api/v1/customer-returns/{created.Id}/confirm", null);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/customer-returns/{created.Id}/confirm", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Receive_Confirmed_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customer-returns:create", "customer-returns:read", "customer-returns:update");
        CustomerReturnDetailDto created = await CreateReturnAndReadAsync(client);
        await client.PostAsync($"/api/v1/customer-returns/{created.Id}/confirm", null);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/customer-returns/{created.Id}/receive", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CustomerReturnDetailDto? body = await response.Content.ReadFromJsonAsync<CustomerReturnDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Received");
        body.ReceivedAtUtc.Should().NotBeNull();
    }

    [Test]
    public async Task Close_Received_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customer-returns:create", "customer-returns:read", "customer-returns:update");
        CustomerReturnDetailDto created = await CreateReturnAndReadAsync(client);
        await client.PostAsync($"/api/v1/customer-returns/{created.Id}/confirm", null);
        await client.PostAsync($"/api/v1/customer-returns/{created.Id}/receive", null);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/customer-returns/{created.Id}/close", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CustomerReturnDetailDto? body = await response.Content.ReadFromJsonAsync<CustomerReturnDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Closed");
        body.ClosedAtUtc.Should().NotBeNull();
    }

    [Test]
    public async Task Cancel_Draft_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customer-returns:create", "customer-returns:read", "customer-returns:update");
        CustomerReturnDetailDto created = await CreateReturnAndReadAsync(client);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/customer-returns/{created.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CustomerReturnDetailDto? body = await response.Content.ReadFromJsonAsync<CustomerReturnDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Cancelled");
    }

    [Test]
    public async Task Cancel_NonDraft_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customer-returns:create", "customer-returns:read", "customer-returns:update");
        CustomerReturnDetailDto created = await CreateReturnAndReadAsync(client);
        await client.PostAsync($"/api/v1/customer-returns/{created.Id}/confirm", null);
        await client.PostAsync($"/api/v1/customer-returns/{created.Id}/receive", null);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/customer-returns/{created.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Create_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = 1,
            Reason = "Test",
            Lines = [new CreateCustomerReturnLineRequest { ProductId = 100, WarehouseId = 1, Quantity = 1m }]
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/customer-returns", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Create_InsufficientPermission_Returns403()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customer-returns:read");
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = 1,
            Reason = "Test",
            Lines = [new CreateCustomerReturnLineRequest { ProductId = 100, WarehouseId = 1, Quantity = 1m }]
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/customer-returns", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
