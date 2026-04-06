using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Services.Stock;
using Warehouse.Inventory.API.Tests.Fixtures;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Inventory.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for stock movement recording and search operations.
/// <para>Links to specification SDD-INV-002.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-002")]
public sealed class StockMovementServiceTests : InventoryTestBase
{
    private StockMovementService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new StockMovementService(Context, Mapper);
    }

    [Test]
    public async Task RecordAsync_ValidRequest_ReturnsMovementAndUpdatesStock()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        Product product = await SeedProductAsync().ConfigureAwait(false);
        RecordStockMovementRequest request = new()
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Quantity = 50m,
            ReasonCode = StockMovementReason.Receipt
        };

        // Act
        Result<StockMovementDto> result = await _sut.RecordAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Quantity.Should().Be(50m);
        result.Value.ReasonCode.Should().Be("Receipt");

        StockLevel? stockLevel = await Context.StockLevels
            .FirstOrDefaultAsync(s => s.ProductId == product.Id && s.WarehouseId == warehouse.Id)
            .ConfigureAwait(false);
        stockLevel.Should().NotBeNull();
        stockLevel!.QuantityOnHand.Should().Be(50m);
    }

    [Test]
    public async Task RecordAsync_ExistingStockLevel_AddsToQuantity()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        Product product = await SeedProductAsync().ConfigureAwait(false);
        await SeedStockLevelAsync(product.Id, warehouse.Id, quantityOnHand: 100m).ConfigureAwait(false);
        RecordStockMovementRequest request = new()
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Quantity = 25m,
            ReasonCode = StockMovementReason.Receipt
        };

        // Act
        Result<StockMovementDto> result = await _sut.RecordAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        StockLevel? stockLevel = await Context.StockLevels
            .FirstOrDefaultAsync(s => s.ProductId == product.Id && s.WarehouseId == warehouse.Id)
            .ConfigureAwait(false);
        stockLevel!.QuantityOnHand.Should().Be(125m);
    }

    [Test]
    public async Task RecordAsync_InvalidProduct_ReturnsError()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        RecordStockMovementRequest request = new()
        {
            ProductId = 9999,
            WarehouseId = warehouse.Id,
            Quantity = 10m,
            ReasonCode = StockMovementReason.Receipt
        };

        // Act
        Result<StockMovementDto> result = await _sut.RecordAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PRODUCT");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task RecordAsync_InvalidWarehouse_ReturnsError()
    {
        // Arrange
        Product product = await SeedProductAsync().ConfigureAwait(false);
        RecordStockMovementRequest request = new()
        {
            ProductId = product.Id,
            WarehouseId = 9999,
            Quantity = 10m,
            ReasonCode = StockMovementReason.Receipt
        };

        // Act
        Result<StockMovementDto> result = await _sut.RecordAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_WAREHOUSE");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task RecordAsync_DeletedProduct_ReturnsInvalidProduct()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        Product product = await SeedProductAsync(isDeleted: true, isActive: false).ConfigureAwait(false);
        RecordStockMovementRequest request = new()
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Quantity = 10m,
            ReasonCode = StockMovementReason.Receipt
        };

        // Act
        Result<StockMovementDto> result = await _sut.RecordAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PRODUCT");
    }

    [Test]
    public async Task SearchAsync_WithProductFilter_ReturnsFilteredResults()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        Product product1 = await SeedProductAsync(code: "MOV-P1").ConfigureAwait(false);
        Product product2 = await SeedProductAsync(code: "MOV-P2").ConfigureAwait(false);

        Context.StockMovements.Add(new StockMovement { ProductId = product1.Id, WarehouseId = warehouse.Id, Quantity = 10m, ReasonCode = StockMovementReason.Receipt, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = 1 });
        Context.StockMovements.Add(new StockMovement { ProductId = product1.Id, WarehouseId = warehouse.Id, Quantity = 20m, ReasonCode = StockMovementReason.Receipt, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = 1 });
        Context.StockMovements.Add(new StockMovement { ProductId = product2.Id, WarehouseId = warehouse.Id, Quantity = 5m, ReasonCode = StockMovementReason.Receipt, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = 1 });
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        SearchStockMovementsRequest request = new() { ProductId = product1.Id, Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<StockMovementDto>> result = await _sut.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
    }
}
