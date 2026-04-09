using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Purchasing.API.Services;
using Warehouse.Purchasing.API.Tests.Fixtures;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for supplier address CRUD operations: create with auto-default, update with default promotion, delete with promotion.
/// <para>Links to specification SDD-PURCH-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class SupplierAddressServiceTests : PurchasingTestBase
{
    private SupplierAddressService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new SupplierAddressService(Context, Mapper);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedAddress()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync().ConfigureAwait(false);
        CreateSupplierAddressRequest request = new()
        {
            AddressType = "Billing",
            StreetLine1 = "123 Main St",
            City = "Sofia",
            PostalCode = "1000",
            CountryCode = "BG"
        };

        // Act
        Result<SupplierAddressDto> result = await _sut.CreateAsync(supplier.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StreetLine1.Should().Be("123 Main St");
        result.Value.City.Should().Be("Sofia");
        result.Value.CountryCode.Should().Be("BG");
        result.Value.AddressType.Should().Be("Billing");
    }

    [Test]
    public async Task CreateAsync_FirstOfType_SetsDefaultTrue()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync().ConfigureAwait(false);
        CreateSupplierAddressRequest request = new()
        {
            AddressType = "Shipping",
            StreetLine1 = "456 Warehouse Rd",
            City = "Plovdiv",
            PostalCode = "4000",
            CountryCode = "BG"
        };

        // Act
        Result<SupplierAddressDto> result = await _sut.CreateAsync(supplier.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_SecondOfType_SetsDefaultFalse()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync().ConfigureAwait(false);
        CreateSupplierAddressRequest firstRequest = new()
        {
            AddressType = "Billing",
            StreetLine1 = "First Billing St",
            City = "Sofia",
            PostalCode = "1000",
            CountryCode = "BG"
        };
        await _sut.CreateAsync(supplier.Id, firstRequest, CancellationToken.None).ConfigureAwait(false);

        CreateSupplierAddressRequest secondRequest = new()
        {
            AddressType = "Billing",
            StreetLine1 = "Second Billing St",
            City = "Varna",
            PostalCode = "9000",
            CountryCode = "BG"
        };

        // Act
        Result<SupplierAddressDto> result = await _sut.CreateAsync(supplier.Id, secondRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDefault.Should().BeFalse();
    }

    [Test]
    public async Task DeleteAsync_DefaultAddress_PromotesNext()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync().ConfigureAwait(false);
        CreateSupplierAddressRequest firstRequest = new()
        {
            AddressType = "Billing",
            StreetLine1 = "First St",
            City = "Sofia",
            PostalCode = "1000",
            CountryCode = "BG"
        };
        Result<SupplierAddressDto> firstResult = await _sut.CreateAsync(supplier.Id, firstRequest, CancellationToken.None).ConfigureAwait(false);
        int firstId = firstResult.Value!.Id;

        CreateSupplierAddressRequest secondRequest = new()
        {
            AddressType = "Billing",
            StreetLine1 = "Second St",
            City = "Plovdiv",
            PostalCode = "4000",
            CountryCode = "BG"
        };
        Result<SupplierAddressDto> secondResult = await _sut.CreateAsync(supplier.Id, secondRequest, CancellationToken.None).ConfigureAwait(false);
        int secondId = secondResult.Value!.Id;

        // Act
        Result deleteResult = await _sut.DeleteAsync(supplier.Id, firstId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();
        SupplierAddress? promoted = await Context.SupplierAddresses.FindAsync(secondId).ConfigureAwait(false);
        promoted.Should().NotBeNull();
        promoted!.IsDefault.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_NonExistentSupplier_ReturnsNotFound()
    {
        // Arrange
        CreateSupplierAddressRequest request = new()
        {
            AddressType = "Billing",
            StreetLine1 = "123 Main St",
            City = "Sofia",
            PostalCode = "1000",
            CountryCode = "BG"
        };

        // Act
        Result<SupplierAddressDto> result = await _sut.CreateAsync(999, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SUPPLIER_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }
}
