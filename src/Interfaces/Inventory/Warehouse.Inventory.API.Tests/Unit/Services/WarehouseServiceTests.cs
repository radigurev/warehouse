using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Services.Warehouse;
using Warehouse.Inventory.API.Tests.Fixtures;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for warehouse management operations: CRUD, search, and soft-delete.
/// <para>Links to specification SDD-INV-003.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-003")]
public sealed class WarehouseServiceTests : InventoryTestBase
{
    private WarehouseService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new WarehouseService(Context, Mapper);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedWarehouse()
    {
        // Arrange
        CreateWarehouseRequest request = new() { Code = "WH-NEW", Name = "New Warehouse", Address = "123 Industrial Rd" };

        // Act
        Result<WarehouseDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Warehouse");
        result.Value.Code.Should().Be("WH-NEW");
    }

    [Test]
    public async Task CreateAsync_DuplicateCode_ReturnsConflict()
    {
        // Arrange
        await SeedWarehouseAsync(code: "WH-DUPE").ConfigureAwait(false);
        CreateWarehouseRequest request = new() { Code = "WH-DUPE", Name = "Another Warehouse" };

        // Act
        Result<WarehouseDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_WAREHOUSE_CODE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task GetByIdAsync_ExistingWarehouse_ReturnsWarehouse()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);

        // Act
        Result<WarehouseDto> result = await _sut.GetByIdAsync(warehouse.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(warehouse.Id);
        result.Value.Name.Should().Be("Main Warehouse");
    }

    [Test]
    public async Task GetByIdAsync_SoftDeletedWarehouse_ReturnsNotFound()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync(isDeleted: true, isActive: false).ConfigureAwait(false);

        // Act
        Result<WarehouseDto> result = await _sut.GetByIdAsync(warehouse.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("WAREHOUSE_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task UpdateAsync_ValidRequest_ReturnsUpdatedWarehouse()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        UpdateWarehouseRequest request = new() { Name = "Updated WH", Address = "456 Updated Rd" };

        // Act
        Result<WarehouseDto> result = await _sut.UpdateAsync(warehouse.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated WH");
    }

    [Test]
    public async Task UpdateAsync_SoftDeletedWarehouse_ReturnsNotFound()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync(isDeleted: true, isActive: false).ConfigureAwait(false);
        UpdateWarehouseRequest request = new() { Name = "Ignored" };

        // Act
        Result<WarehouseDto> result = await _sut.UpdateAsync(warehouse.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("WAREHOUSE_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task DeactivateAsync_ActiveWarehouse_SetsIsDeletedFlags()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);

        // Act
        Result result = await _sut.DeactivateAsync(warehouse.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        WarehouseEntity? updated = await Context.Warehouses.FindAsync(warehouse.Id).ConfigureAwait(false);
        updated!.IsDeleted.Should().BeTrue();
        updated.DeletedAtUtc.Should().NotBeNull();
        updated.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task DeactivateAsync_AlreadyDeleted_ReturnsNotFound()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync(isDeleted: true, isActive: false).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeactivateAsync(warehouse.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("WAREHOUSE_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }
}
