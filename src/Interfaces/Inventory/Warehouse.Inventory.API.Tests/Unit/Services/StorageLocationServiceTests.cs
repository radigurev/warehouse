using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Services.Warehouse;
using Warehouse.Inventory.API.Tests.Fixtures;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for storage location lifecycle operations: CRUD and search.
/// <para>Links to specification SDD-INV-003.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-003")]
public sealed class StorageLocationServiceTests : InventoryTestBase
{
    private StorageLocationService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new StorageLocationService(Context, Mapper);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedLocation()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        Zone zone = await SeedZoneAsync(warehouse.Id).ConfigureAwait(false);
        CreateStorageLocationRequest request = new()
        {
            ZoneId = zone.Id,
            Code = "LOC-A1",
            Name = "Aisle 1 Shelf 1",
            LocationType = "Shelf"
        };

        // Act
        Result<StorageLocationDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("LOC-A1");
        result.Value.Name.Should().Be("Aisle 1 Shelf 1");
    }

    [Test]
    public async Task CreateAsync_InvalidZone_ReturnsError()
    {
        // Arrange
        CreateStorageLocationRequest request = new() { ZoneId = 9999, Code = "LOC-X", Name = "Invalid", LocationType = "Shelf" };

        // Act
        Result<StorageLocationDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_ZONE");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task CreateAsync_DuplicateCodeInWarehouse_ReturnsConflict()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        Zone zone = await SeedZoneAsync(warehouse.Id).ConfigureAwait(false);
        await SeedLocationAsync(warehouse.Id, zone.Id, code: "LOC-DUPE").ConfigureAwait(false);
        CreateStorageLocationRequest request = new() { ZoneId = zone.Id, Code = "LOC-DUPE", Name = "Another", LocationType = "Shelf" };

        // Act
        Result<StorageLocationDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_LOCATION_CODE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task GetByIdAsync_ExistingLocation_ReturnsLocation()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        StorageLocation location = await SeedLocationAsync(warehouse.Id).ConfigureAwait(false);

        // Act
        Result<StorageLocationDto> result = await _sut.GetByIdAsync(location.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(location.Id);
    }

    [Test]
    public async Task GetByIdAsync_NonExistent_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 9999;

        // Act
        Result<StorageLocationDto> result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("LOCATION_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task DeleteAsync_ExistingLocation_DeletesSuccessfully()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        StorageLocation location = await SeedLocationAsync(warehouse.Id).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeleteAsync(location.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        StorageLocation? deleted = await Context.StorageLocations.FindAsync(location.Id).ConfigureAwait(false);
        deleted.Should().BeNull();
    }
}
