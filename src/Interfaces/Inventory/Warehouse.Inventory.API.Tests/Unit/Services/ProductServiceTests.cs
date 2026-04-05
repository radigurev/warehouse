using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Services;
using Warehouse.Inventory.API.Tests.Fixtures;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for product lifecycle operations: CRUD, search, soft-delete, and reactivation.
/// <para>Links to specification SDD-INV-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-001")]
public sealed class ProductServiceTests : InventoryTestBase
{
    private ProductService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new ProductService(Context, Mapper);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedProduct()
    {
        // Arrange
        ProductCategory category = await SeedCategoryAsync().ConfigureAwait(false);
        UnitOfMeasure unit = await SeedUnitOfMeasureAsync().ConfigureAwait(false);
        CreateProductRequest request = new()
        {
            Code = "WIDGET-001",
            Name = "Blue Widget",
            Description = "A blue widget",
            CategoryId = category.Id,
            UnitOfMeasureId = unit.Id,
            Notes = "Important product"
        };

        // Act
        Result<ProductDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Blue Widget");
        result.Value.Code.Should().Be("WIDGET-001");
        result.Value.IsActive.Should().BeTrue();
        result.Value.CategoryName.Should().Be("Electronics");
    }

    [Test]
    public async Task CreateAsync_DuplicateCode_ReturnsConflictError()
    {
        // Arrange
        UnitOfMeasure unit = await SeedUnitOfMeasureAsync().ConfigureAwait(false);
        await SeedProductAsync(code: "DUPE-001", unitOfMeasureId: unit.Id).ConfigureAwait(false);
        CreateProductRequest request = new() { Code = "DUPE-001", Name = "New Product", UnitOfMeasureId = unit.Id };

        // Act
        Result<ProductDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_PRODUCT_CODE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_InvalidCategoryId_ReturnsValidationError()
    {
        // Arrange
        UnitOfMeasure unit = await SeedUnitOfMeasureAsync().ConfigureAwait(false);
        CreateProductRequest request = new() { Code = "NEW-001", Name = "New Product", CategoryId = 9999, UnitOfMeasureId = unit.Id };

        // Act
        Result<ProductDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CATEGORY");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task CreateAsync_InvalidUnitOfMeasureId_ReturnsValidationError()
    {
        // Arrange
        CreateProductRequest request = new() { Code = "NEW-001", Name = "New Product", UnitOfMeasureId = 9999 };

        // Act
        Result<ProductDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_UNIT_OF_MEASURE");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task UpdateAsync_ValidRequest_ReturnsUpdatedProduct()
    {
        // Arrange
        UnitOfMeasure unit = await SeedUnitOfMeasureAsync().ConfigureAwait(false);
        Product product = await SeedProductAsync(unitOfMeasureId: unit.Id).ConfigureAwait(false);
        UpdateProductRequest request = new()
        {
            Name = "Updated Name",
            UnitOfMeasureId = unit.Id,
            Notes = "Updated notes"
        };

        // Act
        Result<ProductDetailDto> result = await _sut.UpdateAsync(product.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Name");
    }

    [Test]
    public async Task UpdateAsync_SoftDeletedProduct_ReturnsNotFound()
    {
        // Arrange
        Product product = await SeedProductAsync(isDeleted: true, isActive: false).ConfigureAwait(false);
        UnitOfMeasure unit = await Context.UnitsOfMeasure.FindAsync(product.UnitOfMeasureId).ConfigureAwait(false) ?? throw new Exception();
        UpdateProductRequest request = new() { Name = "Ignored", UnitOfMeasureId = unit.Id };

        // Act
        Result<ProductDetailDto> result = await _sut.UpdateAsync(product.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task GetByIdAsync_ExistingProduct_ReturnsProduct()
    {
        // Arrange
        UnitOfMeasure unit = await SeedUnitOfMeasureAsync().ConfigureAwait(false);
        ProductCategory category = await SeedCategoryAsync().ConfigureAwait(false);
        Product product = await SeedProductAsync(categoryId: category.Id, unitOfMeasureId: unit.Id).ConfigureAwait(false);

        // Act
        Result<ProductDetailDto> result = await _sut.GetByIdAsync(product.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(product.Id);
        result.Value.Name.Should().Be("Test Product");
        result.Value.CategoryName.Should().Be("Electronics");
    }

    [Test]
    public async Task GetByIdAsync_SoftDeletedProduct_ReturnsNotFound()
    {
        // Arrange
        Product product = await SeedProductAsync(isDeleted: true, isActive: false).ConfigureAwait(false);

        // Act
        Result<ProductDetailDto> result = await _sut.GetByIdAsync(product.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task SearchAsync_WithNameFilter_ReturnsFilteredResults()
    {
        // Arrange
        UnitOfMeasure unit = await SeedUnitOfMeasureAsync().ConfigureAwait(false);
        await SeedProductAsync(code: "P-001", name: "Alpha Widget", unitOfMeasureId: unit.Id).ConfigureAwait(false);
        await SeedProductAsync(code: "P-002", name: "Beta Gadget", unitOfMeasureId: unit.Id).ConfigureAwait(false);
        await SeedProductAsync(code: "P-003", name: "Alpha Gizmo", unitOfMeasureId: unit.Id).ConfigureAwait(false);
        SearchProductsRequest request = new() { Name = "Alpha", Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<ProductDto>> result = await _sut.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().AllSatisfy(p => p.Name.Should().Contain("Alpha"));
    }

    [Test]
    public async Task DeactivateAsync_ActiveProduct_SetsIsDeletedAndDeletedAt()
    {
        // Arrange
        Product product = await SeedProductAsync().ConfigureAwait(false);

        // Act
        Result result = await _sut.DeactivateAsync(product.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Product? updated = await Context.Products.FindAsync(product.Id).ConfigureAwait(false);
        updated!.IsDeleted.Should().BeTrue();
        updated.DeletedAtUtc.Should().NotBeNull();
        updated.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task DeactivateAsync_AlreadyDeleted_ReturnsNotFound()
    {
        // Arrange
        Product product = await SeedProductAsync(isDeleted: true, isActive: false).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeactivateAsync(product.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task ReactivateAsync_SoftDeletedProduct_ClearsDeletedFlags()
    {
        // Arrange
        Product product = await SeedProductAsync(code: "REACT-001", isDeleted: true, isActive: false).ConfigureAwait(false);

        // Act
        Result<ProductDetailDto> result = await _sut.ReactivateAsync(product.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Product? reactivated = await Context.Products.FindAsync(product.Id).ConfigureAwait(false);
        reactivated!.IsDeleted.Should().BeFalse();
        reactivated.DeletedAtUtc.Should().BeNull();
        reactivated.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task ReactivateAsync_AlreadyActiveProduct_ReturnsConflict()
    {
        // Arrange
        Product product = await SeedProductAsync().ConfigureAwait(false);

        // Act
        Result<ProductDetailDto> result = await _sut.ReactivateAsync(product.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PRODUCT_ALREADY_ACTIVE");
        result.StatusCode.Should().Be(409);
    }
}
