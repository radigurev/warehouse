using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Warehouse.Common.Models;
using Warehouse.Purchasing.API.Services;
using Warehouse.Purchasing.API.Tests.Fixtures;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for supplier category operations: CRUD with uniqueness and in-use checks.
/// <para>Links to specification SDD-PURCH-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class SupplierCategoryServiceTests : PurchasingTestBase
{
    private Mock<IDistributedCache> _mockCache = null!;
    private SupplierCategoryService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockCache = new Mock<IDistributedCache>();
        _sut = new SupplierCategoryService(Context, Mapper, _mockCache.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedCategory()
    {
        // Arrange
        CreateSupplierCategoryRequest request = new() { Name = "Electronics", Description = "Electronic components" };

        // Act
        Result<SupplierCategoryDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Electronics");
        result.Value.Description.Should().Be("Electronic components");
    }

    [Test]
    public async Task CreateAsync_DuplicateName_ReturnsConflictError()
    {
        // Arrange
        await SeedCategoryAsync("Chemicals").ConfigureAwait(false);
        CreateSupplierCategoryRequest request = new() { Name = "Chemicals" };

        // Act
        Result<SupplierCategoryDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CATEGORY_NAME");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task UpdateAsync_DuplicateName_ReturnsConflictError()
    {
        // Arrange
        await SeedCategoryAsync("Existing").ConfigureAwait(false);
        SupplierCategory target = await SeedCategoryAsync("Target").ConfigureAwait(false);
        UpdateSupplierCategoryRequest request = new() { Name = "Existing", Description = "Trying to rename" };

        // Act
        Result<SupplierCategoryDto> result = await _sut.UpdateAsync(target.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CATEGORY_NAME");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeleteAsync_CategoryWithSuppliers_ReturnsConflict()
    {
        // Arrange
        SupplierCategory category = await SeedCategoryAsync("InUse").ConfigureAwait(false);
        await SeedSupplierAsync(code: "CAT-S1", categoryId: category.Id).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeleteAsync(category.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CATEGORY_IN_USE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeleteAsync_UnusedCategory_DeletesSuccessfully()
    {
        // Arrange
        SupplierCategory category = await SeedCategoryAsync("Unused").ConfigureAwait(false);

        // Act
        Result result = await _sut.DeleteAsync(category.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        SupplierCategory? deleted = await Context.SupplierCategories.FindAsync(category.Id).ConfigureAwait(false);
        deleted.Should().BeNull();
    }
}
