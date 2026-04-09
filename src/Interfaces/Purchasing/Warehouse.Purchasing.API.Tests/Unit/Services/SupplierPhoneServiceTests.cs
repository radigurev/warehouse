using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Purchasing.API.Services;
using Warehouse.Purchasing.API.Tests.Fixtures;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for supplier phone CRUD operations: create with auto-primary, update with primary promotion, delete with promotion.
/// <para>Links to specification SDD-PURCH-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class SupplierPhoneServiceTests : PurchasingTestBase
{
    private SupplierPhoneService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new SupplierPhoneService(Context, Mapper);
    }

    [Test]
    public async Task CreateAsync_FirstPhone_SetsPrimaryTrue()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync().ConfigureAwait(false);
        CreateSupplierPhoneRequest request = new()
        {
            PhoneType = "Mobile",
            PhoneNumber = "+359888123456"
        };

        // Act
        Result<SupplierPhoneDto> result = await _sut.CreateAsync(supplier.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeTrue();
        result.Value.PhoneNumber.Should().Be("+359888123456");
        result.Value.PhoneType.Should().Be("Mobile");
    }

    [Test]
    public async Task CreateAsync_SecondPhone_SetsPrimaryFalse()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync().ConfigureAwait(false);
        CreateSupplierPhoneRequest firstRequest = new() { PhoneType = "Mobile", PhoneNumber = "+359888111111" };
        await _sut.CreateAsync(supplier.Id, firstRequest, CancellationToken.None).ConfigureAwait(false);

        CreateSupplierPhoneRequest secondRequest = new() { PhoneType = "Landline", PhoneNumber = "+35921234567" };

        // Act
        Result<SupplierPhoneDto> result = await _sut.CreateAsync(supplier.Id, secondRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeFalse();
    }

    [Test]
    public async Task UpdateAsync_SetPrimary_UnsetsOtherPrimary()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync().ConfigureAwait(false);
        CreateSupplierPhoneRequest firstRequest = new() { PhoneType = "Mobile", PhoneNumber = "+359888111111" };
        Result<SupplierPhoneDto> firstResult = await _sut.CreateAsync(supplier.Id, firstRequest, CancellationToken.None).ConfigureAwait(false);
        int firstId = firstResult.Value!.Id;

        CreateSupplierPhoneRequest secondRequest = new() { PhoneType = "Landline", PhoneNumber = "+35921234567" };
        Result<SupplierPhoneDto> secondResult = await _sut.CreateAsync(supplier.Id, secondRequest, CancellationToken.None).ConfigureAwait(false);
        int secondId = secondResult.Value!.Id;

        UpdateSupplierPhoneRequest updateRequest = new()
        {
            PhoneType = "Landline",
            PhoneNumber = "+35921234567",
            IsPrimary = true
        };

        // Act
        Result<SupplierPhoneDto> result = await _sut.UpdateAsync(supplier.Id, secondId, updateRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeTrue();
        SupplierPhone? demoted = await Context.SupplierPhones.FindAsync(firstId).ConfigureAwait(false);
        demoted!.IsPrimary.Should().BeFalse();
    }

    [Test]
    public async Task DeleteAsync_PrimaryPhone_PromotesNext()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync().ConfigureAwait(false);
        CreateSupplierPhoneRequest firstRequest = new() { PhoneType = "Mobile", PhoneNumber = "+359888111111" };
        Result<SupplierPhoneDto> firstResult = await _sut.CreateAsync(supplier.Id, firstRequest, CancellationToken.None).ConfigureAwait(false);
        int firstId = firstResult.Value!.Id;

        CreateSupplierPhoneRequest secondRequest = new() { PhoneType = "Landline", PhoneNumber = "+35921234567" };
        Result<SupplierPhoneDto> secondResult = await _sut.CreateAsync(supplier.Id, secondRequest, CancellationToken.None).ConfigureAwait(false);
        int secondId = secondResult.Value!.Id;

        // Act
        Result deleteResult = await _sut.DeleteAsync(supplier.Id, firstId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();
        SupplierPhone? promoted = await Context.SupplierPhones.FindAsync(secondId).ConfigureAwait(false);
        promoted.Should().NotBeNull();
        promoted!.IsPrimary.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_NonExistentSupplier_ReturnsNotFound()
    {
        // Arrange
        CreateSupplierPhoneRequest request = new() { PhoneType = "Mobile", PhoneNumber = "+359888123456" };

        // Act
        Result<SupplierPhoneDto> result = await _sut.CreateAsync(999, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SUPPLIER_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }
}
