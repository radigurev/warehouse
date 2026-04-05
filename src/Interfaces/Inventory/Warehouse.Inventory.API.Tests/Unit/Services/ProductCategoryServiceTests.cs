using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Services.Products;
using Warehouse.Inventory.API.Tests.Fixtures;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for product category operations: CRUD with uniqueness and in-use checks.
/// <para>Links to specification SDD-INV-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-001")]
public sealed class ProductCategoryServiceTests : InventoryTestBase
{
    private ProductCategoryService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new ProductCategoryService(Context, Mapper);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedCategory()
    {
        // Arrange
        CreateProductCategoryRequest request = new() { Name = "Fasteners", Description = "Nuts, bolts, screws" };

        // Act
        Result<ProductCategoryDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Fasteners");
    }

    [Test]
    public async Task CreateAsync_DuplicateName_ReturnsConflict()
    {
        // Arrange
        await SeedCategoryAsync("Hardware").ConfigureAwait(false);
        CreateProductCategoryRequest request = new() { Name = "Hardware" };

        // Act
        Result<ProductCategoryDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CATEGORY_NAME");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeleteAsync_CategoryWithProducts_ReturnsConflict()
    {
        // Arrange
        ProductCategory category = await SeedCategoryAsync("InUse").ConfigureAwait(false);
        await SeedProductAsync(code: "CAT-P1", categoryId: category.Id).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeleteAsync(category.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CATEGORY_IN_USE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeleteAsync_CategoryWithChildren_ReturnsConflict()
    {
        // Arrange
        ProductCategory parent = await SeedCategoryAsync("ParentCat").ConfigureAwait(false);
        ProductCategory child = new() { Name = "ChildCat", ParentCategoryId = parent.Id, CreatedAtUtc = DateTime.UtcNow };
        Context.ProductCategories.Add(child);
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeleteAsync(parent.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CATEGORY_HAS_CHILDREN");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeleteAsync_UnusedCategory_DeletesSuccessfully()
    {
        // Arrange
        ProductCategory category = await SeedCategoryAsync("Unused").ConfigureAwait(false);

        // Act
        Result result = await _sut.DeleteAsync(category.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task UpdateAsync_SelfParent_ReturnsBadRequest()
    {
        // Arrange
        ProductCategory category = await SeedCategoryAsync("SelfRef").ConfigureAwait(false);
        UpdateProductCategoryRequest request = new() { Name = "SelfRef", ParentCategoryId = category.Id };

        // Act
        Result<ProductCategoryDto> result = await _sut.UpdateAsync(category.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CATEGORY_SELF_PARENT");
        result.StatusCode.Should().Be(400);
    }
}
