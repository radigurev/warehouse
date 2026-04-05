using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Services;
using Warehouse.Inventory.API.Tests.Fixtures;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for zone lifecycle operations: CRUD and search.
/// <para>Links to specification SDD-INV-003.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-003")]
public sealed class ZoneServiceTests : InventoryTestBase
{
    private ZoneService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new ZoneService(Context, Mapper);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedZone()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        CreateZoneRequest request = new() { WarehouseId = warehouse.Id, Code = "Z-01", Name = "Receiving Zone" };

        // Act
        Result<ZoneDetailDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("Z-01");
        result.Value.Name.Should().Be("Receiving Zone");
    }

    [Test]
    public async Task CreateAsync_InvalidWarehouse_ReturnsError()
    {
        // Arrange
        CreateZoneRequest request = new() { WarehouseId = 9999, Code = "Z-01", Name = "Zone" };

        // Act
        Result<ZoneDetailDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_WAREHOUSE");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task CreateAsync_DuplicateCodeInWarehouse_ReturnsConflict()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        await SeedZoneAsync(warehouse.Id, "Z-DUPE", "Zone Dupe").ConfigureAwait(false);
        CreateZoneRequest request = new() { WarehouseId = warehouse.Id, Code = "Z-DUPE", Name = "Another Zone" };

        // Act
        Result<ZoneDetailDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_ZONE_CODE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task GetByIdAsync_ExistingZone_ReturnsZone()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        Zone zone = await SeedZoneAsync(warehouse.Id).ConfigureAwait(false);

        // Act
        Result<ZoneDetailDto> result = await _sut.GetByIdAsync(zone.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(zone.Id);
    }

    [Test]
    public async Task GetByIdAsync_NonExistent_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 9999;

        // Act
        Result<ZoneDetailDto> result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ZONE_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task DeleteAsync_ExistingZone_DeletesSuccessfully()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        Zone zone = await SeedZoneAsync(warehouse.Id).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeleteAsync(zone.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Zone? deleted = await Context.Zones.FindAsync(zone.Id).ConfigureAwait(false);
        deleted.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_NonExistent_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 9999;

        // Act
        Result result = await _sut.DeleteAsync(nonExistentId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ZONE_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }
}
