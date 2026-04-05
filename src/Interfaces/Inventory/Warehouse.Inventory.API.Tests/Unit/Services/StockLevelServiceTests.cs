using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Services.Stock;
using Warehouse.Inventory.API.Tests.Fixtures;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for stock level query operations: get, search, and summary.
/// <para>Links to specification SDD-INV-002.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-002")]
public sealed class StockLevelServiceTests : InventoryTestBase
{
    private StockLevelService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new StockLevelService(Context, Mapper);
    }

    [Test]
    public async Task GetByIdAsync_ExistingStockLevel_ReturnsStockLevel()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        Product product = await SeedProductAsync().ConfigureAwait(false);
        StockLevel stockLevel = await SeedStockLevelAsync(product.Id, warehouse.Id, quantityOnHand: 250m).ConfigureAwait(false);

        // Act
        Result<StockLevelDto> result = await _sut.GetByIdAsync(stockLevel.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(stockLevel.Id);
    }

    [Test]
    public async Task GetByIdAsync_NonExistent_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 9999;

        // Act
        Result<StockLevelDto> result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("STOCK_LEVEL_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task GetSummaryByProductAsync_ExistingProduct_ReturnsSummary()
    {
        // Arrange
        WarehouseEntity wh1 = await SeedWarehouseAsync(code: "WH-SUM-1", name: "Warehouse 1").ConfigureAwait(false);
        WarehouseEntity wh2 = await SeedWarehouseAsync(code: "WH-SUM-2", name: "Warehouse 2").ConfigureAwait(false);
        Product product = await SeedProductAsync(code: "SUM-PROD").ConfigureAwait(false);
        await SeedStockLevelAsync(product.Id, wh1.Id, quantityOnHand: 100m, quantityReserved: 10m).ConfigureAwait(false);
        await SeedStockLevelAsync(product.Id, wh2.Id, quantityOnHand: 50m, quantityReserved: 5m).ConfigureAwait(false);

        // Act
        Result<StockSummaryDto> result = await _sut.GetSummaryByProductAsync(product.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalOnHand.Should().Be(150m);
        result.Value.TotalReserved.Should().Be(15m);
        result.Value.TotalAvailable.Should().Be(135m);
        result.Value.WarehouseBreakdown.Should().HaveCount(2);
    }

    [Test]
    public async Task GetSummaryByProductAsync_DeletedProduct_ReturnsNotFound()
    {
        // Arrange
        Product product = await SeedProductAsync(isDeleted: true, isActive: false).ConfigureAwait(false);

        // Act
        Result<StockSummaryDto> result = await _sut.GetSummaryByProductAsync(product.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task SearchAsync_WithWarehouseFilter_ReturnsFilteredResults()
    {
        // Arrange
        WarehouseEntity wh1 = await SeedWarehouseAsync(code: "WH-SRCH-1", name: "WH Search 1").ConfigureAwait(false);
        WarehouseEntity wh2 = await SeedWarehouseAsync(code: "WH-SRCH-2", name: "WH Search 2").ConfigureAwait(false);
        Product product = await SeedProductAsync(code: "SRCH-PROD").ConfigureAwait(false);
        await SeedStockLevelAsync(product.Id, wh1.Id, quantityOnHand: 100m).ConfigureAwait(false);
        await SeedStockLevelAsync(product.Id, wh2.Id, quantityOnHand: 200m).ConfigureAwait(false);
        SearchStockLevelsRequest request = new() { WarehouseId = wh1.Id, Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<StockLevelDto>> result = await _sut.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
    }
}
