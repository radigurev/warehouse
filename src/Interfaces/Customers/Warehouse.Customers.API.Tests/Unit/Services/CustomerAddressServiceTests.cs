using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Warehouse.Common.Models;
using Warehouse.Customers.API.Services;
using Warehouse.Customers.API.Tests.Fixtures;
using Warehouse.Customers.DBModel.Models;
using Warehouse.Infrastructure.Caching;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for customer address operations.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CustomerAddressServiceTests : CustomerTestBase
{
    private Mock<INomenclatureResolver> _nomenclatureResolverMock = null!;
    private CustomerAddressService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _nomenclatureResolverMock = new Mock<INomenclatureResolver>();
        _sut = new CustomerAddressService(Context, Mapper, _nomenclatureResolverMock.Object);
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
}
