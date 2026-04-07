using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Services;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="CarrierService"/> covering CRUD, deactivation, and service levels.
/// <para>Linked to SDD-FULF-001 section 2.6.</para>
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
public sealed class CarrierServiceTests : FulfillmentTestBase
{
    private Mock<IDistributedCache> _mockCache = null!;
    private CarrierService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockCache = new Mock<IDistributedCache>();
        _sut = new CarrierService(Context, Mapper, _mockCache.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedCarrier()
    {
        // Arrange
        CreateCarrierRequest request = new() { Code = "FEDEX", Name = "FedEx International" };

        // Act
        Result<CarrierDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Code, Is.EqualTo("FEDEX"));
            Assert.That(result.Value.Name, Is.EqualTo("FedEx International"));
            Assert.That(result.Value.IsActive, Is.True);
        });
    }

    [Test]
    public async Task CreateAsync_DuplicateCode_ReturnsConflictError()
    {
        // Arrange
        await SeedCarrierAsync(code: "DHL");
        CreateCarrierRequest request = new() { Code = "DHL", Name = "DHL Duplicate" };

        // Act
        Result<CarrierDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("DUPLICATE_CARRIER_CODE"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task CreateAsync_InvalidatesCacheAfterCreate()
    {
        // Arrange
        CreateCarrierRequest request = new() { Code = "UPS", Name = "UPS Ground" };

        // Act
        await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        _mockCache.Verify(
            c => c.RemoveAsync(It.IsAny<string>(), CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_ExistingCarrier_ReturnsCarrierWithServiceLevels()
    {
        // Arrange
        Carrier carrier = await SeedCarrierAsync();
        await SeedCarrierServiceLevelAsync(carrier.Id, code: "EXPRESS", name: "Express Delivery");

        // Act
        Result<CarrierDetailDto> result = await _sut.GetByIdAsync(carrier.Id, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.ServiceLevels, Has.Count.EqualTo(1));
            Assert.That(result.Value.ServiceLevels.First().Code, Is.EqualTo("EXPRESS"));
        });
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 999;

        // Act
        Result<CarrierDetailDto> result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("CARRIER_NOT_FOUND"));
            Assert.That(result.StatusCode, Is.EqualTo(404));
        });
    }

    [Test]
    public async Task UpdateAsync_ValidRequest_ReturnsUpdatedCarrier()
    {
        // Arrange
        Carrier carrier = await SeedCarrierAsync(code: "DHL", name: "DHL Express");
        UpdateCarrierRequest request = new() { Name = "DHL Worldwide", IsActive = true, ContactEmail = "info@dhl.com" };

        // Act
        Result<CarrierDetailDto> result = await _sut.UpdateAsync(carrier.Id, request, 2, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Name, Is.EqualTo("DHL Worldwide"));
            Assert.That(result.Value.ContactEmail, Is.EqualTo("info@dhl.com"));
        });
    }

    [Test]
    public async Task DeactivateAsync_ActiveCarrier_SetsInactive()
    {
        // Arrange
        Carrier carrier = await SeedCarrierAsync(isActive: true);

        // Act
        Result<CarrierDetailDto> result = await _sut.DeactivateAsync(carrier.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.IsActive, Is.False);
        });
    }

    [Test]
    public async Task DeactivateAsync_CarrierWithActiveShipments_ReturnsConflict()
    {
        // Arrange
        Carrier carrier = await SeedCarrierAsync(isActive: true);
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));
        await SeedShipmentAsync(so.Id, status: nameof(ShipmentStatus.Dispatched), carrierId: carrier.Id);

        // Act
        Result<CarrierDetailDto> result = await _sut.DeactivateAsync(carrier.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("CARRIER_HAS_ACTIVE_SHIPMENTS"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task SearchAsync_ByName_ReturnsFilteredResults()
    {
        // Arrange
        await SeedCarrierAsync(code: "DHL", name: "DHL Express");
        await SeedCarrierAsync(code: "FEDEX", name: "FedEx International");
        SearchCarriersRequest request = new() { Name = "DHL", Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<CarrierDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.TotalCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task CreateServiceLevelAsync_ValidRequest_ReturnsCreatedServiceLevel()
    {
        // Arrange
        Carrier carrier = await SeedCarrierAsync();
        CreateCarrierServiceLevelRequest request = new() { Code = "NEXT_DAY", Name = "Next Day Air", EstimatedDeliveryDays = 1, BaseRate = 25.00m };

        // Act
        Result<CarrierServiceLevelDto> result = await _sut.CreateServiceLevelAsync(carrier.Id, request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Code, Is.EqualTo("NEXT_DAY"));
            Assert.That(result.Value.Name, Is.EqualTo("Next Day Air"));
            Assert.That(result.Value.BaseRate, Is.EqualTo(25.00m));
        });
    }

    [Test]
    public async Task CreateServiceLevelAsync_DuplicateCodeForCarrier_ReturnsConflictError()
    {
        // Arrange
        Carrier carrier = await SeedCarrierAsync();
        await SeedCarrierServiceLevelAsync(carrier.Id, code: "EXPRESS");
        CreateCarrierServiceLevelRequest request = new() { Code = "EXPRESS", Name = "Express Dup" };

        // Act
        Result<CarrierServiceLevelDto> result = await _sut.CreateServiceLevelAsync(carrier.Id, request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("DUPLICATE_SERVICE_LEVEL_CODE"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task CreateServiceLevelAsync_NonExistentCarrier_ReturnsNotFound()
    {
        // Arrange
        CreateCarrierServiceLevelRequest request = new() { Code = "EXPR", Name = "Express" };

        // Act
        Result<CarrierServiceLevelDto> result = await _sut.CreateServiceLevelAsync(999, request, CancellationToken.None);

        // Assert
        Assert.That(result.ErrorCode, Is.EqualTo("CARRIER_NOT_FOUND"));
    }

    [Test]
    public async Task DeleteServiceLevelAsync_UnusedServiceLevel_DeletesSuccessfully()
    {
        // Arrange
        Carrier carrier = await SeedCarrierAsync();
        CarrierServiceLevel level = await SeedCarrierServiceLevelAsync(carrier.Id);

        // Act
        Result result = await _sut.DeleteServiceLevelAsync(carrier.Id, level.Id, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DeleteServiceLevelAsync_ServiceLevelWithShipments_ReturnsConflict()
    {
        // Arrange
        Carrier carrier = await SeedCarrierAsync();
        CarrierServiceLevel level = await SeedCarrierServiceLevelAsync(carrier.Id);
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));
        Shipment shipment = await SeedShipmentAsync(so.Id, carrierId: carrier.Id);
        shipment.CarrierServiceLevelId = level.Id;
        await Context.SaveChangesAsync(CancellationToken.None);

        // Act
        Result result = await _sut.DeleteServiceLevelAsync(carrier.Id, level.Id, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SERVICE_LEVEL_IN_USE"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task ListServiceLevelsAsync_CarrierWithLevels_ReturnsAllLevels()
    {
        // Arrange
        Carrier carrier = await SeedCarrierAsync();
        await SeedCarrierServiceLevelAsync(carrier.Id, code: "STD", name: "Standard");
        await SeedCarrierServiceLevelAsync(carrier.Id, code: "EXP", name: "Express");

        // Act
        Result<IReadOnlyList<CarrierServiceLevelDto>> result = await _sut.ListServiceLevelsAsync(carrier.Id, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Has.Count.EqualTo(2));
        });
    }
}
