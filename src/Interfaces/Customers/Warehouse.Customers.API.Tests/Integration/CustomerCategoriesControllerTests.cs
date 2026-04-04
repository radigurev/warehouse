using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Customers.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Integration;

/// <summary>
/// Integration tests for the CustomerCategoriesController: create, list, update, and delete.
/// <para>Covers SDD-CUST-001 sections 2.4 (Customer Categories) and error rules.</para>
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
[Category("Integration")]
public sealed class CustomerCategoriesControllerTests : CustomerApiTestBase
{
    private HttpClient _client = null!;

    /// <summary>
    /// Creates an authenticated client before each test.
    /// </summary>
    [SetUp]
    public override async Task SetUpAsync()
    {
        await base.SetUpAsync();
        _client = CreateAuthenticatedClient(
            "customers:write", "customers:read", "customers:update", "customers:delete");
    }

    [Test]
    public async Task CreateCategory_ValidPayload_Returns201()
    {
        // Arrange
        CreateCategoryRequest request = new()
        {
            Name = "Wholesale",
            Description = "Wholesale buyers"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/v1/customer-categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CustomerCategoryDto? body = await response.Content.ReadFromJsonAsync<CustomerCategoryDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Wholesale");
        body.Description.Should().Be("Wholesale buyers");
    }

    [Test]
    public async Task CreateCategory_DuplicateName_Returns409()
    {
        // Arrange
        await CreateCategoryViaApiAsync(_client, name: "Retail");

        CreateCategoryRequest request = new()
        {
            Name = "Retail"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/v1/customer-categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }

    [Test]
    public async Task ListCategories_Returns200()
    {
        // Arrange
        await CreateCategoryViaApiAsync(_client, name: "Wholesale");
        await CreateCategoryViaApiAsync(_client, name: "Retail");
        await CreateCategoryViaApiAsync(_client, name: "Government");

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/v1/customer-categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<CustomerCategoryDto>? body = await response.Content
            .ReadFromJsonAsync<List<CustomerCategoryDto>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(3);
    }

    [Test]
    public async Task UpdateCategory_ValidPayload_Returns200()
    {
        // Arrange
        CustomerCategoryDto created = await CreateCategoryAndReadAsync(_client, name: "Old Name");

        UpdateCategoryRequest request = new()
        {
            Name = "New Name",
            Description = "Updated description"
        };

        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/v1/customer-categories/{created.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CustomerCategoryDto? body = await response.Content.ReadFromJsonAsync<CustomerCategoryDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("New Name");
        body.Description.Should().Be("Updated description");
    }

    [Test]
    public async Task DeleteCategory_WithCustomers_Returns409()
    {
        // Arrange
        CustomerCategoryDto category = await CreateCategoryAndReadAsync(_client, name: "In Use Category");
        await CreateCustomerViaApiAsync(_client, name: "Linked Corp", categoryId: category.Id);

        // Act
        HttpResponseMessage response = await _client.DeleteAsync(
            $"/api/v1/customer-categories/{category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }

    [Test]
    public async Task DeleteCategory_Unused_Returns204()
    {
        // Arrange
        CustomerCategoryDto category = await CreateCategoryAndReadAsync(_client, name: "Unused Category");

        // Act
        HttpResponseMessage response = await _client.DeleteAsync(
            $"/api/v1/customer-categories/{category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
