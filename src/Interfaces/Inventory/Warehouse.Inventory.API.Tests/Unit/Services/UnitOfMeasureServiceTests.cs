using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Services.Products;
using Warehouse.Inventory.API.Tests.Fixtures;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for unit of measure operations: CRUD with uniqueness and in-use checks.
/// <para>Links to specification SDD-INV-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-001")]
public sealed class UnitOfMeasureServiceTests : InventoryTestBase
{
    private UnitOfMeasureService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new UnitOfMeasureService(Context, Mapper);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedUnit()
    {
        // Arrange
        CreateUnitOfMeasureRequest request = new() { Code = "KG", Name = "Kilograms" };

        // Act
        Result<UnitOfMeasureDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("KG");
        result.Value.Name.Should().Be("Kilograms");
    }

    [Test]
    public async Task CreateAsync_DuplicateCode_ReturnsConflict()
    {
        // Arrange
        await SeedUnitOfMeasureAsync("PCS", "Pieces").ConfigureAwait(false);
        CreateUnitOfMeasureRequest request = new() { Code = "PCS", Name = "Pieces Duplicate" };

        // Act
        Result<UnitOfMeasureDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_UNIT_CODE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeleteAsync_UnitWithProducts_ReturnsConflict()
    {
        // Arrange
        UnitOfMeasure unit = await SeedUnitOfMeasureAsync("M", "Meters").ConfigureAwait(false);
        await SeedProductAsync(code: "UOM-P1", unitOfMeasureId: unit.Id).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeleteAsync(unit.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("UNIT_IN_USE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeleteAsync_UnusedUnit_DeletesSuccessfully()
    {
        // Arrange
        UnitOfMeasure unit = await SeedUnitOfMeasureAsync("LTR", "Liters").ConfigureAwait(false);

        // Act
        Result result = await _sut.DeleteAsync(unit.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task GetByIdAsync_NonExistent_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 9999;

        // Act
        Result<UnitOfMeasureDto> result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("UNIT_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }
}
