using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.Customers.API.Services;
using Warehouse.Customers.API.Tests.Fixtures;
using Warehouse.Customers.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for contact information operations: addresses, phones, and emails.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CustomerContactServiceTests : CustomerTestBase
{
    private CustomerContactService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new CustomerContactService(Context, Mapper);
    }

    [Test]
    public async Task CreateAddress_ValidRequest_ReturnsCreatedAddress()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CreateAddressRequest request = new()
        {
            AddressType = "Billing",
            StreetLine1 = "456 Oak Ave",
            City = "Plovdiv",
            PostalCode = "4000",
            CountryCode = "BG"
        };

        // Act
        Result<CustomerAddressDto> result = await _sut.CreateAddressAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StreetLine1.Should().Be("456 Oak Ave");
        result.Value.City.Should().Be("Plovdiv");
        result.Value.CountryCode.Should().Be("BG");
    }

    [Test]
    public async Task CreateAddress_FirstOfType_SetsDefaultTrue()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CreateAddressRequest request = new()
        {
            AddressType = "Shipping",
            StreetLine1 = "789 Elm St",
            City = "Varna",
            PostalCode = "9000",
            CountryCode = "BG"
        };

        // Act
        Result<CustomerAddressDto> result = await _sut.CreateAddressAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAddress_DefaultAddress_PromotesNext()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CustomerAddress firstAddr = await SeedAddressAsync(customer.Id, "Billing", isDefault: true).ConfigureAwait(false);

        CustomerAddress secondAddr = new()
        {
            CustomerId = customer.Id,
            AddressType = "Billing",
            StreetLine1 = "Second Street",
            City = "Burgas",
            PostalCode = "8000",
            CountryCode = "BG",
            IsDefault = false,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(1)
        };
        Context.CustomerAddresses.Add(secondAddr);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeleteAddressAsync(customer.Id, firstAddr.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        CustomerAddress? promoted = await Context.CustomerAddresses.FindAsync(secondAddr.Id).ConfigureAwait(false);
        promoted!.IsDefault.Should().BeTrue();
    }

    [Test]
    public async Task CreatePhone_FirstPhone_SetsPrimaryTrue()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CreatePhoneRequest request = new()
        {
            PhoneType = "Mobile",
            PhoneNumber = "+359888000111"
        };

        // Act
        Result<CustomerPhoneDto> result = await _sut.CreatePhoneAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeTrue();
    }

    [Test]
    public async Task UpdatePhone_SetPrimary_UnsetsOtherPrimary()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CustomerPhone primary = await SeedPhoneAsync(customer.Id, "Mobile", "+359888111222", isPrimary: true).ConfigureAwait(false);
        CustomerPhone secondary = await SeedPhoneAsync(customer.Id, "Landline", "+35932111222", isPrimary: false).ConfigureAwait(false);
        UpdatePhoneRequest request = new()
        {
            PhoneType = "Landline",
            PhoneNumber = "+35932111222",
            IsPrimary = true
        };

        // Act
        Result<CustomerPhoneDto> result = await _sut.UpdatePhoneAsync(customer.Id, secondary.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeTrue();

        CustomerPhone? oldPrimary = await Context.CustomerPhones.FindAsync(primary.Id).ConfigureAwait(false);
        oldPrimary!.IsPrimary.Should().BeFalse();
    }

    [Test]
    public async Task DeletePhone_PrimaryPhone_PromotesNext()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CustomerPhone primary = await SeedPhoneAsync(customer.Id, "Mobile", "+359888111222", isPrimary: true).ConfigureAwait(false);

        CustomerPhone second = new()
        {
            CustomerId = customer.Id,
            PhoneType = "Landline",
            PhoneNumber = "+35932111222",
            IsPrimary = false,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(1)
        };
        Context.CustomerPhones.Add(second);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeletePhoneAsync(customer.Id, primary.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        CustomerPhone? promoted = await Context.CustomerPhones.FindAsync(second.Id).ConfigureAwait(false);
        promoted!.IsPrimary.Should().BeTrue();
    }

    [Test]
    public async Task CreateEmail_ValidRequest_ReturnsCreatedEmail()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CreateEmailRequest request = new()
        {
            EmailType = "General",
            EmailAddress = "info@acme.com"
        };

        // Act
        Result<CustomerEmailDto> result = await _sut.CreateEmailAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.EmailAddress.Should().Be("info@acme.com");
        result.Value.EmailType.Should().Be("General");
    }

    [Test]
    public async Task CreateEmail_DuplicateForCustomer_ReturnsConflict()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        await SeedEmailAsync(customer.Id, "General", "dupe@acme.com").ConfigureAwait(false);
        CreateEmailRequest request = new() { EmailType = "Billing", EmailAddress = "dupe@acme.com" };

        // Act
        Result<CustomerEmailDto> result = await _sut.CreateEmailAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CUSTOMER_EMAIL");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateEmail_FirstEmail_SetsPrimaryTrue()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CreateEmailRequest request = new()
        {
            EmailType = "General",
            EmailAddress = "first@acme.com"
        };

        // Act
        Result<CustomerEmailDto> result = await _sut.CreateEmailAsync(customer.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeTrue();
    }

    [Test]
    public async Task UpdateEmail_DuplicateForCustomer_ReturnsConflict()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        await SeedEmailAsync(customer.Id, "General", "existing@acme.com", isPrimary: true).ConfigureAwait(false);
        CustomerEmail emailToUpdate = await SeedEmailAsync(customer.Id, "Billing", "other@acme.com").ConfigureAwait(false);
        UpdateEmailRequest request = new()
        {
            EmailType = "Billing",
            EmailAddress = "existing@acme.com",
            IsPrimary = false
        };

        // Act
        Result<CustomerEmailDto> result = await _sut.UpdateEmailAsync(customer.Id, emailToUpdate.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CUSTOMER_EMAIL");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeleteEmail_PrimaryEmail_PromotesNext()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync().ConfigureAwait(false);
        CustomerEmail primary = await SeedEmailAsync(customer.Id, "General", "primary@acme.com", isPrimary: true).ConfigureAwait(false);

        CustomerEmail second = new()
        {
            CustomerId = customer.Id,
            EmailType = "Billing",
            EmailAddress = "secondary@acme.com",
            IsPrimary = false,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(1)
        };
        Context.CustomerEmails.Add(second);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeleteEmailAsync(customer.Id, primary.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        CustomerEmail? promoted = await Context.CustomerEmails.FindAsync(second.Id).ConfigureAwait(false);
        promoted!.IsPrimary.Should().BeTrue();
    }
}
