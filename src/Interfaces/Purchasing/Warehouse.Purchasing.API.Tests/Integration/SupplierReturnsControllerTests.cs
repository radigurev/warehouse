using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Warehouse.Purchasing.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Tests.Integration;

/// <summary>
/// Integration tests for the SupplierReturnsController: create, search, get, confirm, and cancel.
/// <para>Covers SDD-PURCH-001 supplier return operations.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
[Category("Integration")]
public sealed class SupplierReturnsControllerTests : PurchasingApiTestBase
{
    private static readonly string[] AllPermissions =
    [
        "suppliers:create", "suppliers:read",
        "supplier-returns:create", "supplier-returns:read", "supplier-returns:update"
    ];

    [Test]
    public async Task Create_Valid_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Return Supplier");

        // Act
        HttpResponseMessage response = await CreateSupplierReturnViaApiAsync(client, supplier.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SupplierReturnDetailDto? body = await response.Content.ReadFromJsonAsync<SupplierReturnDetailDto>();
        body.Should().NotBeNull();
        body!.SupplierId.Should().Be(supplier.Id);
        body.Status.Should().Be("Draft");
        body.Lines.Should().HaveCount(1);
    }

    [Test]
    public async Task Create_EmptyLines_Returns422Or400()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Empty Return Supplier");

        CreateSupplierReturnRequest request = new()
        {
            SupplierId = supplier.Id,
            Reason = "Defective goods",
            Lines = []
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/supplier-returns", request);

        // Assert
        HttpStatusCode statusCode = response.StatusCode;
        bool isValidationError = statusCode == HttpStatusCode.BadRequest
                              || statusCode == HttpStatusCode.UnprocessableEntity;
        isValidationError.Should().BeTrue($"expected 400 or 422 but got {(int)statusCode}");
    }

    [Test]
    public async Task Get_Existing_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Get Return Supplier");
        HttpResponseMessage createResponse = await CreateSupplierReturnViaApiAsync(client, supplier.Id);
        SupplierReturnDetailDto created = (await createResponse.Content.ReadFromJsonAsync<SupplierReturnDetailDto>())!;

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/supplier-returns/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SupplierReturnDetailDto? body = await response.Content.ReadFromJsonAsync<SupplierReturnDetailDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
    }

    [Test]
    public async Task Search_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Search Return Supplier");
        await CreateSupplierReturnViaApiAsync(client, supplier.Id);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/supplier-returns");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<SupplierReturnDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<SupplierReturnDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Test]
    public async Task Confirm_Draft_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Confirm Return Supplier");
        HttpResponseMessage createResponse = await CreateSupplierReturnViaApiAsync(client, supplier.Id);
        SupplierReturnDetailDto created = (await createResponse.Content.ReadFromJsonAsync<SupplierReturnDetailDto>())!;

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/supplier-returns/{created.Id}/confirm", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SupplierReturnDetailDto? body = await response.Content.ReadFromJsonAsync<SupplierReturnDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Confirmed");
        body.ConfirmedAtUtc.Should().NotBeNull();
    }

    [Test]
    public async Task Confirm_NonDraft_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Double Confirm Return");
        HttpResponseMessage createResponse = await CreateSupplierReturnViaApiAsync(client, supplier.Id);
        SupplierReturnDetailDto created = (await createResponse.Content.ReadFromJsonAsync<SupplierReturnDetailDto>())!;
        await client.PostAsync($"/api/v1/supplier-returns/{created.Id}/confirm", null);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/supplier-returns/{created.Id}/confirm", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Cancel_Draft_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Cancel Return Supplier");
        HttpResponseMessage createResponse = await CreateSupplierReturnViaApiAsync(client, supplier.Id);
        SupplierReturnDetailDto created = (await createResponse.Content.ReadFromJsonAsync<SupplierReturnDetailDto>())!;

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/supplier-returns/{created.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SupplierReturnDetailDto? body = await response.Content.ReadFromJsonAsync<SupplierReturnDetailDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Cancelled");
    }

    [Test]
    public async Task Cancel_NonDraft_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient(AllPermissions);
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Cancel Confirmed Return");
        HttpResponseMessage createResponse = await CreateSupplierReturnViaApiAsync(client, supplier.Id);
        SupplierReturnDetailDto created = (await createResponse.Content.ReadFromJsonAsync<SupplierReturnDetailDto>())!;
        await client.PostAsync($"/api/v1/supplier-returns/{created.Id}/confirm", null);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/supplier-returns/{created.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Create_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = 1,
            Reason = "Defective",
            Lines = [new CreateSupplierReturnLineRequest { ProductId = 1, WarehouseId = 1, Quantity = 1m }]
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/supplier-returns", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Create_InsufficientPermission_Returns403()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("supplier-returns:read");
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = 1,
            Reason = "Defective",
            Lines = [new CreateSupplierReturnLineRequest { ProductId = 1, WarehouseId = 1, Quantity = 1m }]
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/supplier-returns", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
