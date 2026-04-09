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
/// Integration tests for the SuppliersController: create, search, get, update, delete, and reactivate.
/// <para>Covers SDD-PURCH-001 supplier lifecycle operations.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
[Category("Integration")]
public sealed class SuppliersControllerTests : PurchasingApiTestBase
{
    [Test]
    public async Task Create_ValidPayload_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read");
        CreateSupplierRequest request = new()
        {
            Name = "Acme Materials Ltd",
            Code = "SUPP-CREATE-001",
            TaxId = "BG111222333"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/suppliers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SupplierDetailDto? body = await response.Content.ReadFromJsonAsync<SupplierDetailDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Acme Materials Ltd");
        body.Code.Should().Be("SUPP-CREATE-001");
        body.TaxId.Should().Be("BG111222333");
        body.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task Create_DuplicateCode_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read");
        await CreateSupplierViaApiAsync(client, name: "First Supplier", code: "SUPP-DUP-001");

        CreateSupplierRequest request = new()
        {
            Name = "Second Supplier",
            Code = "SUPP-DUP-001"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/suppliers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }

    [Test]
    public async Task Create_DuplicateTaxId_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read");
        await CreateSupplierViaApiAsync(client, name: "First Supplier", taxId: "BG999888777");

        CreateSupplierRequest request = new()
        {
            Name = "Second Supplier",
            Code = "SUPP-TAX-002",
            TaxId = "BG999888777"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/suppliers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }

    [Test]
    public async Task Create_InvalidPayload_Returns400()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create");
        CreateSupplierRequest request = new()
        {
            Name = ""
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/suppliers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        CreateSupplierRequest request = new()
        {
            Name = "Unauth Supplier"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/suppliers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Create_InsufficientPermission_Returns403()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:read");
        CreateSupplierRequest request = new()
        {
            Name = "Forbidden Supplier"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/suppliers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Get_ExistingId_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read");
        SupplierDetailDto created = await CreateSupplierAndReadAsync(client, name: "Findme Supplier");

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/suppliers/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SupplierDetailDto? body = await response.Content.ReadFromJsonAsync<SupplierDetailDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
        body.Name.Should().Be("Findme Supplier");
    }

    [Test]
    public async Task Get_NonExistent_Returns404()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:read");

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/suppliers/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Search_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read");
        await CreateSupplierAndReadAsync(client, name: "Search Alpha Supplier");
        await CreateSupplierAndReadAsync(client, name: "Search Beta Supplier");

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/suppliers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<SupplierDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<SupplierDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Test]
    public async Task Search_WithNameFilter_ReturnsFiltered()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read");
        await CreateSupplierAndReadAsync(client, name: "FilterAlpha Corp");
        await CreateSupplierAndReadAsync(client, name: "FilterBeta Industries");
        await CreateSupplierAndReadAsync(client, name: "FilterAlpha Solutions");

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/suppliers?Name=FilterAlpha");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<SupplierDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<SupplierDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(2);
        body.Items.Should().OnlyContain(s => s.Name.Contains("FilterAlpha"));
    }

    [Test]
    public async Task Update_ValidPayload_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierDetailDto created = await CreateSupplierAndReadAsync(client, name: "Old Supplier Name");

        UpdateSupplierRequest request = new()
        {
            Name = "New Supplier Name",
            Notes = "Updated notes"
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/suppliers/{created.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SupplierDetailDto? body = await response.Content.ReadFromJsonAsync<SupplierDetailDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("New Supplier Name");
        body.Notes.Should().Be("Updated notes");
    }

    [Test]
    public async Task Update_SoftDeleted_Returns404()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update", "suppliers:delete");
        SupplierDetailDto created = await CreateSupplierAndReadAsync(client, name: "Deleted Supplier");
        await client.DeleteAsync($"/api/v1/suppliers/{created.Id}");

        UpdateSupplierRequest request = new()
        {
            Name = "Should Fail"
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/suppliers/{created.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Delete_Active_Returns204()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:delete");
        SupplierDetailDto created = await CreateSupplierAndReadAsync(client, name: "Doomed Supplier");

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/suppliers/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Delete_AlreadyDeleted_Returns404()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:delete");
        SupplierDetailDto created = await CreateSupplierAndReadAsync(client, name: "Double Delete Supplier");
        await client.DeleteAsync($"/api/v1/suppliers/{created.Id}");

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/suppliers/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Reactivate_SoftDeleted_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update", "suppliers:delete");
        SupplierDetailDto created = await CreateSupplierAndReadAsync(client, name: "Revive Supplier");
        await client.DeleteAsync($"/api/v1/suppliers/{created.Id}");

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/suppliers/{created.Id}/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SupplierDetailDto? body = await response.Content.ReadFromJsonAsync<SupplierDetailDto>();
        body.Should().NotBeNull();
        body!.IsActive.Should().BeTrue();
        body.Name.Should().Be("Revive Supplier");
    }

    [Test]
    public async Task Reactivate_Active_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierDetailDto created = await CreateSupplierAndReadAsync(client, name: "Already Active Supplier");

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/suppliers/{created.Id}/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
