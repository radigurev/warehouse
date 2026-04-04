using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Warehouse.Customers.API.Tests.Fixtures;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Integration;

/// <summary>
/// Integration tests for the CustomerContactsController: addresses, phones, and emails CRUD.
/// <para>Covers SDD-CUST-001 sections 2.3 (Customer Contacts) and error rules.</para>
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
[Category("Integration")]
public sealed class CustomerContactsControllerTests : CustomerApiTestBase
{
    private HttpClient _client = null!;
    private int _customerId;

    /// <summary>
    /// Creates an authenticated client and seeds a customer before each test.
    /// </summary>
    [SetUp]
    public override async Task SetUpAsync()
    {
        await base.SetUpAsync();
        _client = CreateAuthenticatedClient("customers:write", "customers:read", "customers:update");
        CustomerDetailDto customer = await CreateCustomerAndReadAsync(_client, name: "Contact Test Corp");
        _customerId = customer.Id;
    }

    [Test]
    public async Task CreateAddress_ValidPayload_Returns201()
    {
        // Arrange
        CreateAddressRequest request = new()
        {
            AddressType = "Billing",
            StreetLine1 = "456 Oak Ave",
            City = "Plovdiv",
            PostalCode = "4000",
            CountryCode = "BG"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/customers/{_customerId}/addresses", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CustomerAddressDto? body = await response.Content.ReadFromJsonAsync<CustomerAddressDto>();
        body.Should().NotBeNull();
        body!.AddressType.Should().Be("Billing");
        body.City.Should().Be("Plovdiv");
        body.CountryCode.Should().Be("BG");
    }

    [Test]
    public async Task ListAddresses_ReturnsAllForCustomer()
    {
        // Arrange
        await CreateAddressViaApiAsync(_client, _customerId, addressType: "Billing", city: "Sofia");
        await CreateAddressViaApiAsync(_client, _customerId, addressType: "Shipping", city: "Plovdiv");

        // Act
        HttpResponseMessage response = await _client.GetAsync(
            $"/api/v1/customers/{_customerId}/addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<CustomerAddressDto>? body = await response.Content
            .ReadFromJsonAsync<List<CustomerAddressDto>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(2);
    }

    [Test]
    public async Task UpdateAddress_ValidPayload_Returns200()
    {
        // Arrange
        HttpResponseMessage createResponse = await CreateAddressViaApiAsync(
            _client, _customerId, addressType: "Billing");
        CustomerAddressDto? created = await createResponse.Content
            .ReadFromJsonAsync<CustomerAddressDto>();

        UpdateAddressRequest request = new()
        {
            AddressType = "Shipping",
            StreetLine1 = "789 New Street",
            City = "Varna",
            PostalCode = "9000",
            CountryCode = "BG",
            IsDefault = true
        };

        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/v1/customers/{_customerId}/addresses/{created!.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CustomerAddressDto? body = await response.Content.ReadFromJsonAsync<CustomerAddressDto>();
        body.Should().NotBeNull();
        body!.City.Should().Be("Varna");
        body.AddressType.Should().Be("Shipping");
    }

    [Test]
    public async Task DeleteAddress_Returns204()
    {
        // Arrange
        HttpResponseMessage createResponse = await CreateAddressViaApiAsync(
            _client, _customerId, addressType: "Billing");
        CustomerAddressDto? created = await createResponse.Content
            .ReadFromJsonAsync<CustomerAddressDto>();

        // Act
        HttpResponseMessage response = await _client.DeleteAsync(
            $"/api/v1/customers/{_customerId}/addresses/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task CreatePhone_ValidPayload_Returns201()
    {
        // Arrange
        CreatePhoneRequest request = new()
        {
            PhoneType = "Mobile",
            PhoneNumber = "+359888999888"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/customers/{_customerId}/phones", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CustomerPhoneDto? body = await response.Content.ReadFromJsonAsync<CustomerPhoneDto>();
        body.Should().NotBeNull();
        body!.PhoneType.Should().Be("Mobile");
        body.PhoneNumber.Should().Be("+359888999888");
    }

    [Test]
    public async Task ListPhones_ReturnsAllForCustomer()
    {
        // Arrange
        await CreatePhoneViaApiAsync(_client, _customerId, phoneNumber: "+359888111111");
        await CreatePhoneViaApiAsync(_client, _customerId, phoneType: "Landline", phoneNumber: "+35932111222");

        // Act
        HttpResponseMessage response = await _client.GetAsync(
            $"/api/v1/customers/{_customerId}/phones");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<CustomerPhoneDto>? body = await response.Content
            .ReadFromJsonAsync<List<CustomerPhoneDto>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(2);
    }

    [Test]
    public async Task DeletePhone_Returns204()
    {
        // Arrange
        HttpResponseMessage createResponse = await CreatePhoneViaApiAsync(
            _client, _customerId, phoneNumber: "+359888333333");
        CustomerPhoneDto? created = await createResponse.Content
            .ReadFromJsonAsync<CustomerPhoneDto>();

        // Act
        HttpResponseMessage response = await _client.DeleteAsync(
            $"/api/v1/customers/{_customerId}/phones/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task CreateEmail_ValidPayload_Returns201()
    {
        // Arrange
        CreateEmailRequest request = new()
        {
            EmailType = "General",
            EmailAddress = "contact@testcorp.com"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/customers/{_customerId}/emails", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CustomerEmailDto? body = await response.Content.ReadFromJsonAsync<CustomerEmailDto>();
        body.Should().NotBeNull();
        body!.EmailType.Should().Be("General");
        body.EmailAddress.Should().Be("contact@testcorp.com");
    }

    [Test]
    public async Task CreateEmail_DuplicateForCustomer_Returns409()
    {
        // Arrange
        await CreateEmailViaApiAsync(_client, _customerId, emailAddress: "same@testcorp.com");

        CreateEmailRequest request = new()
        {
            EmailType = "Billing",
            EmailAddress = "same@testcorp.com"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/customers/{_customerId}/emails", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task ListEmails_ReturnsAllForCustomer()
    {
        // Arrange
        await CreateEmailViaApiAsync(_client, _customerId, emailAddress: "one@testcorp.com");
        await CreateEmailViaApiAsync(_client, _customerId, emailType: "Billing", emailAddress: "two@testcorp.com");

        // Act
        HttpResponseMessage response = await _client.GetAsync(
            $"/api/v1/customers/{_customerId}/emails");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<CustomerEmailDto>? body = await response.Content
            .ReadFromJsonAsync<List<CustomerEmailDto>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(2);
    }

    [Test]
    public async Task DeleteEmail_Returns204()
    {
        // Arrange
        HttpResponseMessage createResponse = await CreateEmailViaApiAsync(
            _client, _customerId, emailAddress: "delete@testcorp.com");
        CustomerEmailDto? created = await createResponse.Content
            .ReadFromJsonAsync<CustomerEmailDto>();

        // Act
        HttpResponseMessage response = await _client.DeleteAsync(
            $"/api/v1/customers/{_customerId}/emails/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
