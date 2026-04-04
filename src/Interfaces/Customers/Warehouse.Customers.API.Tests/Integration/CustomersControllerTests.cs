using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Customers.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Customers.API.Tests.Integration;

/// <summary>
/// Integration tests for the CustomersController: create, search, get, update, delete, and reactivate.
/// <para>Covers SDD-CUST-001 sections 2.1 (Customer Lifecycle) and error rules.</para>
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
[Category("Integration")]
public sealed class CustomersControllerTests : CustomerApiTestBase
{
    [Test]
    public async Task CreateCustomer_ValidPayload_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:write", "customers:read");
        CreateCustomerRequest request = new()
        {
            Name = "Test Corp",
            Code = "CUST-INT-001",
            TaxId = "BG123456789"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/customers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CustomerDetailDto? body = await response.Content.ReadFromJsonAsync<CustomerDetailDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Test Corp");
        body.TaxId.Should().Be("BG123456789");
        body.IsActive.Should().BeTrue();
        body.Code.Should().Be("CUST-INT-001");
    }

    [Test]
    public async Task CreateCustomer_DuplicateCode_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:write", "customers:read");
        await CreateCustomerViaApiAsync(client, name: "First Corp", code: "CUST-DUP-001");

        CreateCustomerRequest request = new()
        {
            Name = "Second Corp",
            Code = "CUST-DUP-001"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/customers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }

    [Test]
    public async Task CreateCustomer_DuplicateTaxId_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:write", "customers:read");
        await CreateCustomerViaApiAsync(client, name: "First Corp", taxId: "BG999999999");

        CreateCustomerRequest request = new()
        {
            Name = "Second Corp",
            Code = "CUST-TAX-002",
            TaxId = "BG999999999"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/customers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
    }

    [Test]
    public async Task CreateCustomer_InvalidPayload_Returns400ProblemDetails()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:write");
        CreateCustomerRequest request = new()
        {
            Name = ""
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/customers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateCustomer_Unauthenticated_Returns401()
    {
        // Arrange
        HttpClient client = CreateClient();
        CreateCustomerRequest request = new()
        {
            Name = "Unauth Corp"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/customers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateCustomer_InsufficientPermissions_Returns403()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:read");
        CreateCustomerRequest request = new()
        {
            Name = "Forbidden Corp"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/customers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetCustomer_ExistingId_Returns200WithDetails()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:write", "customers:read");
        CustomerDetailDto created = await CreateCustomerAndReadAsync(client, name: "Findme Corp");

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/customers/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CustomerDetailDto? body = await response.Content.ReadFromJsonAsync<CustomerDetailDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
        body.Name.Should().Be("Findme Corp");
    }

    [Test]
    public async Task GetCustomer_SoftDeletedId_Returns404()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:write", "customers:read", "customers:delete");
        CustomerDetailDto created = await CreateCustomerAndReadAsync(client, name: "Delete Me Corp");
        await client.DeleteAsync($"/api/v1/customers/{created.Id}");

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/customers/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetCustomer_NonExistentId_Returns404()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:read");

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/customers/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateCustomer_ValidPayload_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:write", "customers:read", "customers:update");
        CustomerDetailDto created = await CreateCustomerAndReadAsync(client, name: "Old Name Corp");

        UpdateCustomerRequest request = new()
        {
            Name = "New Name Corp",
            Notes = "Updated notes"
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/customers/{created.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CustomerDetailDto? body = await response.Content.ReadFromJsonAsync<CustomerDetailDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("New Name Corp");
        body.Notes.Should().Be("Updated notes");
    }

    [Test]
    public async Task UpdateCustomer_SoftDeletedCustomer_Returns404()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:write", "customers:read", "customers:update", "customers:delete");
        CustomerDetailDto created = await CreateCustomerAndReadAsync(client, name: "Deleted Corp");
        await client.DeleteAsync($"/api/v1/customers/{created.Id}");

        UpdateCustomerRequest request = new()
        {
            Name = "Should Fail Corp"
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/customers/{created.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteCustomer_ActiveCustomer_Returns204()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:write", "customers:read", "customers:delete");
        CustomerDetailDto created = await CreateCustomerAndReadAsync(client, name: "Doomed Corp");

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/customers/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteCustomer_AlreadyDeleted_Returns404()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:write", "customers:read", "customers:delete");
        CustomerDetailDto created = await CreateCustomerAndReadAsync(client, name: "Double Delete Corp");
        await client.DeleteAsync($"/api/v1/customers/{created.Id}");

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/customers/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task SearchCustomers_WithNameFilter_ReturnsMatchingResults()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:write", "customers:read");
        await CreateCustomerAndReadAsync(client, name: "Alpha Corp");
        await CreateCustomerAndReadAsync(client, name: "Beta Industries");
        await CreateCustomerAndReadAsync(client, name: "Alpha Solutions");

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/customers?Name=Alpha");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<CustomerDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<CustomerDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(2);
        body.Items.Should().OnlyContain(c => c.Name.Contains("Alpha"));
    }

    [Test]
    public async Task SearchCustomers_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:write", "customers:read");
        for (int i = 1; i <= 5; i++)
            await CreateCustomerAndReadAsync(client, name: $"Paged Corp {i}");

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/customers?Page=2&PageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaginatedResponse<CustomerDto>? body = await response.Content
            .ReadFromJsonAsync<PaginatedResponse<CustomerDto>>();
        body.Should().NotBeNull();
        body!.Page.Should().Be(2);
        body.PageSize.Should().Be(2);
        body.Items.Should().HaveCountLessOrEqualTo(2);
        body.TotalCount.Should().BeGreaterOrEqualTo(5);
    }

    [Test]
    public async Task ReactivateCustomer_SoftDeleted_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("customers:write", "customers:read", "customers:update", "customers:delete");
        CustomerDetailDto created = await CreateCustomerAndReadAsync(client, name: "Revive Corp");
        await client.DeleteAsync($"/api/v1/customers/{created.Id}");

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/customers/{created.Id}/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CustomerDetailDto? body = await response.Content.ReadFromJsonAsync<CustomerDetailDto>();
        body.Should().NotBeNull();
        body!.IsActive.Should().BeTrue();
        body.Name.Should().Be("Revive Corp");
    }
}
