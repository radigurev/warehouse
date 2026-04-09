using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Warehouse.Purchasing.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Tests.Integration;

/// <summary>
/// Integration tests for the SupplierContactsController: addresses, phones, and emails CRUD.
/// <para>Covers SDD-PURCH-001 supplier contact management operations.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
[Category("Integration")]
public sealed class SupplierContactsControllerTests : PurchasingApiTestBase
{
    [Test]
    public async Task CreateAddress_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Addr Supplier");

        CreateSupplierAddressRequest request = new()
        {
            AddressType = "Billing",
            StreetLine1 = "123 Main St",
            City = "Sofia",
            PostalCode = "1000",
            CountryCode = "BG"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/addresses", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SupplierAddressDto? body = await response.Content.ReadFromJsonAsync<SupplierAddressDto>();
        body.Should().NotBeNull();
        body!.StreetLine1.Should().Be("123 Main St");
        body.City.Should().Be("Sofia");
        body.CountryCode.Should().Be("BG");
    }

    [Test]
    public async Task ListAddresses_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "List Addr Supplier");

        CreateSupplierAddressRequest request = new()
        {
            AddressType = "Shipping",
            StreetLine1 = "456 Side Rd",
            City = "Plovdiv",
            PostalCode = "4000",
            CountryCode = "BG"
        };
        await client.PostAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/addresses", request);

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/suppliers/{supplier.Id}/addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        IReadOnlyList<SupplierAddressDto>? body = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<SupplierAddressDto>>();
        body.Should().NotBeNull();
        body.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Test]
    public async Task UpdateAddress_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Update Addr Supplier");

        CreateSupplierAddressRequest createReq = new()
        {
            AddressType = "Billing",
            StreetLine1 = "Old Street",
            City = "Sofia",
            PostalCode = "1000",
            CountryCode = "BG"
        };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/addresses", createReq);
        SupplierAddressDto createdAddr = (await createResponse.Content.ReadFromJsonAsync<SupplierAddressDto>())!;

        UpdateSupplierAddressRequest updateReq = new()
        {
            AddressType = "Billing",
            StreetLine1 = "New Street 42",
            City = "Varna",
            PostalCode = "9000",
            CountryCode = "BG"
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/addresses/{createdAddr.Id}", updateReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SupplierAddressDto? body = await response.Content.ReadFromJsonAsync<SupplierAddressDto>();
        body.Should().NotBeNull();
        body!.StreetLine1.Should().Be("New Street 42");
        body.City.Should().Be("Varna");
    }

    [Test]
    public async Task DeleteAddress_Returns204()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Del Addr Supplier");

        CreateSupplierAddressRequest createReq = new()
        {
            AddressType = "Shipping",
            StreetLine1 = "Delete Me St",
            City = "Burgas",
            PostalCode = "8000",
            CountryCode = "BG"
        };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/addresses", createReq);
        SupplierAddressDto createdAddr = (await createResponse.Content.ReadFromJsonAsync<SupplierAddressDto>())!;

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/suppliers/{supplier.Id}/addresses/{createdAddr.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task CreatePhone_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Phone Supplier");

        CreateSupplierPhoneRequest request = new()
        {
            PhoneType = "Mobile",
            PhoneNumber = "+359888123456"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/phones", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SupplierPhoneDto? body = await response.Content.ReadFromJsonAsync<SupplierPhoneDto>();
        body.Should().NotBeNull();
        body!.PhoneNumber.Should().Be("+359888123456");
        body.PhoneType.Should().Be("Mobile");
    }

    [Test]
    public async Task ListPhones_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "List Phone Supplier");

        CreateSupplierPhoneRequest phoneReq = new()
        {
            PhoneType = "Landline",
            PhoneNumber = "+35921234567"
        };
        await client.PostAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/phones", phoneReq);

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/suppliers/{supplier.Id}/phones");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        IReadOnlyList<SupplierPhoneDto>? body = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<SupplierPhoneDto>>();
        body.Should().NotBeNull();
        body.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Test]
    public async Task DeletePhone_Returns204()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Del Phone Supplier");

        CreateSupplierPhoneRequest phoneReq = new()
        {
            PhoneType = "Fax",
            PhoneNumber = "+35929876543"
        };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/phones", phoneReq);
        SupplierPhoneDto createdPhone = (await createResponse.Content.ReadFromJsonAsync<SupplierPhoneDto>())!;

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/suppliers/{supplier.Id}/phones/{createdPhone.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task CreateEmail_Returns201()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Email Supplier");

        CreateSupplierEmailRequest request = new()
        {
            EmailType = "General",
            EmailAddress = "contact@supplier.com"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/emails", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SupplierEmailDto? body = await response.Content.ReadFromJsonAsync<SupplierEmailDto>();
        body.Should().NotBeNull();
        body!.EmailAddress.Should().Be("contact@supplier.com");
        body.EmailType.Should().Be("General");
    }

    [Test]
    public async Task CreateEmail_Duplicate_Returns409()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Dup Email Supplier");

        CreateSupplierEmailRequest request = new()
        {
            EmailType = "General",
            EmailAddress = "duplicate@supplier.com"
        };
        await client.PostAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/emails", request);

        CreateSupplierEmailRequest duplicateReq = new()
        {
            EmailType = "Billing",
            EmailAddress = "duplicate@supplier.com"
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/emails", duplicateReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task ListEmails_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "List Email Supplier");

        CreateSupplierEmailRequest emailReq = new()
        {
            EmailType = "Billing",
            EmailAddress = "billing@supplier.com"
        };
        await client.PostAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/emails", emailReq);

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/suppliers/{supplier.Id}/emails");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        IReadOnlyList<SupplierEmailDto>? body = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<SupplierEmailDto>>();
        body.Should().NotBeNull();
        body.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Test]
    public async Task UpdateEmail_Returns200()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Update Email Supplier");

        CreateSupplierEmailRequest createReq = new()
        {
            EmailType = "General",
            EmailAddress = "old@supplier.com"
        };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/emails", createReq);
        SupplierEmailDto createdEmail = (await createResponse.Content.ReadFromJsonAsync<SupplierEmailDto>())!;

        UpdateSupplierEmailRequest updateReq = new()
        {
            EmailType = "Billing",
            EmailAddress = "new@supplier.com",
            IsPrimary = true
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/emails/{createdEmail.Id}", updateReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SupplierEmailDto? body = await response.Content.ReadFromJsonAsync<SupplierEmailDto>();
        body.Should().NotBeNull();
        body!.EmailAddress.Should().Be("new@supplier.com");
        body.EmailType.Should().Be("Billing");
    }

    [Test]
    public async Task DeleteEmail_Returns204()
    {
        // Arrange
        HttpClient client = CreateAuthenticatedClient("suppliers:create", "suppliers:read", "suppliers:update");
        SupplierDetailDto supplier = await CreateSupplierAndReadAsync(client, name: "Del Email Supplier");

        CreateSupplierEmailRequest createReq = new()
        {
            EmailType = "Support",
            EmailAddress = "support@supplier.com"
        };
        HttpResponseMessage createResponse = await client.PostAsJsonAsync($"/api/v1/suppliers/{supplier.Id}/emails", createReq);
        SupplierEmailDto createdEmail = (await createResponse.Content.ReadFromJsonAsync<SupplierEmailDto>())!;

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/suppliers/{supplier.Id}/emails/{createdEmail.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
