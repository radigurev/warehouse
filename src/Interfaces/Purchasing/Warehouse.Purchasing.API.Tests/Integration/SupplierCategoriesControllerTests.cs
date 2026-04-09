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
/// Integration tests for the SupplierCategoriesController: create, list, get, update, and delete.
/// <para>Covers SDD-PURCH-001 supplier category management operations.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
[Category("Integration")]
public sealed class SupplierCategoriesControllerTests : PurchasingApiTestBase
{
    [Test]
    public async Task Create_Valid_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read");
        CreateSupplierCategoryRequest request = new()
        {
            Name = "Raw Materials",
            Description = "Suppliers of raw materials"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/supplier-categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SupplierCategoryDto? body = await response.Content.ReadFromJsonAsync<SupplierCategoryDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Raw Materials");
        body.Description.Should().Be("Suppliers of raw materials");
    }

    [Test]
    public async Task Create_DuplicateName_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read");
        await CreateCategoryViaApiAsync(client, name: "Duplicate Category");

        CreateSupplierCategoryRequest request = new()
        {
            Name = "Duplicate Category"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/supplier-categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }

    [Test]
    public async Task List_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read");
        await CreateCategoryAndReadAsync(client, name: "Cat A");
        await CreateCategoryAndReadAsync(client, name: "Cat B");

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/supplier-categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<SupplierCategoryDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<SupplierCategoryDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Test]
    public async Task Get_Existing_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read");
        SupplierCategoryDto created = await CreateCategoryAndReadAsync(client, name: "Findme Category");

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/supplier-categories/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SupplierCategoryDto? body = await response.Content.ReadFromJsonAsync<SupplierCategoryDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
        body.Name.Should().Be("Findme Category");
    }

    [Test]
    public async Task Update_Valid_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierCategoryDto created = await CreateCategoryAndReadAsync(client, name: "Old Cat Name");

        UpdateSupplierCategoryRequest request = new()
        {
            Name = "New Cat Name",
            Description = "Updated description"
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/supplier-categories/{created.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SupplierCategoryDto? body = await response.Content.ReadFromJsonAsync<SupplierCategoryDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("New Cat Name");
        body.Description.Should().Be("Updated description");
    }

    [Test]
    public async Task Delete_Unused_Returns204()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:delete");
        SupplierCategoryDto created = await CreateCategoryAndReadAsync(client, name: "Unused Category");

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/supplier-categories/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Delete_WithSuppliers_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:delete");
        SupplierCategoryDto category = await CreateCategoryAndReadAsync(client, name: "Used Category");
        await CreateSupplierAndReadAsync(client, name: "Linked Supplier", categoryId: category.Id);

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/supplier-categories/{category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Create_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        CreateSupplierCategoryRequest request = new()
        {
            Name = "Unauth Category"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/supplier-categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
