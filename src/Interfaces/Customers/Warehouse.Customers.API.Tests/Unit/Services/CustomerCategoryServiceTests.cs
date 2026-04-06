using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Warehouse.Common.Models;
using Warehouse.Customers.API.Services;
using Warehouse.Customers.API.Tests.Fixtures;
using Warehouse.Customers.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Customers;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for customer category operations: CRUD with uniqueness and in-use checks.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CustomerCategoryServiceTests : CustomerTestBase
{
    private Mock<IDistributedCache> _mockCache = null!;
    private CustomerCategoryService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockCache = new Mock<IDistributedCache>();
        _sut = new CustomerCategoryService(Context, Mapper, _mockCache.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedCategory()
    {
        // Arrange
        CreateCategoryRequest request = new() { Name = "VIP", Description = "High-value customers" };

        // Act
        Result<CustomerCategoryDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("VIP");
        result.Value.Description.Should().Be("High-value customers");
        result.Value.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task CreateAsync_DuplicateName_ReturnsConflictError()
    {
        // Arrange
        await SeedCategoryAsync("Wholesale").ConfigureAwait(false);
        CreateCategoryRequest request = new() { Name = "Wholesale" };

        // Act
        Result<CustomerCategoryDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CATEGORY_NAME");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task UpdateAsync_DuplicateName_ReturnsConflictError()
    {
        // Arrange
        await SeedCategoryAsync("Retail").ConfigureAwait(false);
        CustomerCategory target = await SeedCategoryAsync("Wholesale").ConfigureAwait(false);
        UpdateCategoryRequest request = new() { Name = "Retail" };

        // Act
        Result<CustomerCategoryDto> result = await _sut.UpdateAsync(target.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CATEGORY_NAME");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeleteAsync_CategoryWithCustomers_ReturnsConflict()
    {
        // Arrange
        CustomerCategory category = await SeedCategoryAsync("InUse").ConfigureAwait(false);
        await SeedCustomerAsync(categoryId: category.Id).ConfigureAwait(false);

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
        CustomerCategory category = await SeedCategoryAsync("Unused").ConfigureAwait(false);

        // Act
        Result result = await _sut.DeleteAsync(category.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        CustomerCategory? deleted = await Context.CustomerCategories.FindAsync(category.Id).ConfigureAwait(false);
        deleted.Should().BeNull();
    }
}
