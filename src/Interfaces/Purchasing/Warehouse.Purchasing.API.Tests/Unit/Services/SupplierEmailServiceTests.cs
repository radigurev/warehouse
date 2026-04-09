using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Purchasing.API.Services;
using Warehouse.Purchasing.API.Tests.Fixtures;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for supplier email CRUD operations: create with auto-primary, duplicate detection, update, delete with promotion.
/// <para>Links to specification SDD-PURCH-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class SupplierEmailServiceTests : PurchasingTestBase
{
    private SupplierEmailService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new SupplierEmailService(Context, Mapper);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedEmail()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync().ConfigureAwait(false);
        CreateSupplierEmailRequest request = new()
        {
            EmailType = "General",
            EmailAddress = "info@supplier.com"
        };

        // Act
        Result<SupplierEmailDto> result = await _sut.CreateAsync(supplier.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.EmailAddress.Should().Be("info@supplier.com");
        result.Value.EmailType.Should().Be("General");
    }

    [Test]
    public async Task CreateAsync_FirstEmail_SetsPrimaryTrue()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync().ConfigureAwait(false);
        CreateSupplierEmailRequest request = new()
        {
            EmailType = "General",
            EmailAddress = "first@supplier.com"
        };

        // Act
        Result<SupplierEmailDto> result = await _sut.CreateAsync(supplier.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_DuplicateForSupplier_ReturnsConflict()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync().ConfigureAwait(false);
        CreateSupplierEmailRequest request = new()
        {
            EmailType = "General",
            EmailAddress = "dup@supplier.com"
        };
        await _sut.CreateAsync(supplier.Id, request, CancellationToken.None).ConfigureAwait(false);

        CreateSupplierEmailRequest duplicateRequest = new()
        {
            EmailType = "Billing",
            EmailAddress = "dup@supplier.com"
        };

        // Act
        Result<SupplierEmailDto> result = await _sut.CreateAsync(supplier.Id, duplicateRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_SUPPLIER_EMAIL");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task UpdateAsync_DuplicateForSupplier_ReturnsConflict()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync().ConfigureAwait(false);
        CreateSupplierEmailRequest firstRequest = new() { EmailType = "General", EmailAddress = "first@supplier.com" };
        await _sut.CreateAsync(supplier.Id, firstRequest, CancellationToken.None).ConfigureAwait(false);

        CreateSupplierEmailRequest secondRequest = new() { EmailType = "Billing", EmailAddress = "second@supplier.com" };
        Result<SupplierEmailDto> secondResult = await _sut.CreateAsync(supplier.Id, secondRequest, CancellationToken.None).ConfigureAwait(false);
        int secondId = secondResult.Value!.Id;

        UpdateSupplierEmailRequest updateRequest = new()
        {
            EmailType = "Billing",
            EmailAddress = "first@supplier.com",
            IsPrimary = false
        };

        // Act
        Result<SupplierEmailDto> result = await _sut.UpdateAsync(supplier.Id, secondId, updateRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_SUPPLIER_EMAIL");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeleteAsync_PrimaryEmail_PromotesNext()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync().ConfigureAwait(false);
        CreateSupplierEmailRequest firstRequest = new() { EmailType = "General", EmailAddress = "primary@supplier.com" };
        Result<SupplierEmailDto> firstResult = await _sut.CreateAsync(supplier.Id, firstRequest, CancellationToken.None).ConfigureAwait(false);
        int firstId = firstResult.Value!.Id;

        CreateSupplierEmailRequest secondRequest = new() { EmailType = "Billing", EmailAddress = "billing@supplier.com" };
        Result<SupplierEmailDto> secondResult = await _sut.CreateAsync(supplier.Id, secondRequest, CancellationToken.None).ConfigureAwait(false);
        int secondId = secondResult.Value!.Id;

        // Act
        Result deleteResult = await _sut.DeleteAsync(supplier.Id, firstId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();
        SupplierEmail? promoted = await Context.SupplierEmails.FindAsync(secondId).ConfigureAwait(false);
        promoted.Should().NotBeNull();
        promoted!.IsPrimary.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_NonExistentSupplier_ReturnsNotFound()
    {
        // Arrange
        CreateSupplierEmailRequest request = new() { EmailType = "General", EmailAddress = "test@supplier.com" };

        // Act
        Result<SupplierEmailDto> result = await _sut.CreateAsync(999, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SUPPLIER_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }
}
