using MassTransit;
using Moq;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Fulfillment.API.Services;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.Infrastructure.Caching;
using Warehouse.Infrastructure.Correlation;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="ShipmentService"/> covering dispatch, status transitions, and event publishing.
/// <para>Linked to SDD-FULF-001 sections 2.4 and 2.5.</para>
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
public sealed class ShipmentServiceTests : FulfillmentTestBase
{
    private Mock<IPublishEndpoint> _mockPublishEndpoint = null!;
    private Mock<ICorrelationIdAccessor> _mockCorrelationIdAccessor = null!;
    private Mock<IFulfillmentEventService> _mockEventService = null!;
    private Mock<INomenclatureResolver> _mockNomenclatureResolver = null!;
    private ShipmentService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockCorrelationIdAccessor = new Mock<ICorrelationIdAccessor>();
        _mockEventService = new Mock<IFulfillmentEventService>();
        _mockNomenclatureResolver = new Mock<INomenclatureResolver>();
        _sut = new ShipmentService(Context, Mapper, _mockPublishEndpoint.Object, _mockCorrelationIdAccessor.Object, _mockEventService.Object, _mockNomenclatureResolver.Object);
    }

    [Test]
    public async Task CreateAsync_PackedSO_ReturnsCreatedShipment()
    {
        // Arrange
        SalesOrder so = await SeedPackedSalesOrderAsync();
        CreateShipmentRequest request = new() { SalesOrderId = so.Id };

        // Act
        Result<ShipmentDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.ShipmentNumber, Does.StartWith("SH-"));
            Assert.That(result.Value.Status, Is.EqualTo(nameof(ShipmentStatus.Dispatched)));
        });
    }

    [Test]
    public async Task CreateAsync_NonPackedSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Confirmed));
        CreateShipmentRequest request = new() { SalesOrderId = so.Id };

        // Act
        Result<ShipmentDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_NOT_DISPATCHABLE"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task CreateAsync_AlreadyShippedSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedPackedSalesOrderAsync();
        await SeedShipmentAsync(so.Id);
        CreateShipmentRequest request = new() { SalesOrderId = so.Id };

        // Act
        Result<ShipmentDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_ALREADY_SHIPPED"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task CreateAsync_EmptyParcel_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Packed));
        await SeedParcelAsync(so.Id);
        CreateShipmentRequest request = new() { SalesOrderId = so.Id };

        // Act
        Result<ShipmentDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("EMPTY_PARCEL"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task CreateAsync_TransitionsSOToShipped()
    {
        // Arrange
        SalesOrder so = await SeedPackedSalesOrderAsync();
        CreateShipmentRequest request = new() { SalesOrderId = so.Id };

        // Act
        await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        await Context.Entry(so).ReloadAsync(CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(so.Status, Is.EqualTo(nameof(SalesOrderStatus.Shipped)));
            Assert.That(so.ShippedAtUtc, Is.Not.Null);
        });
    }

    [Test]
    public async Task CreateAsync_PublishesShipmentDispatchedEvent()
    {
        // Arrange
        SalesOrder so = await SeedPackedSalesOrderAsync();
        CreateShipmentRequest request = new() { SalesOrderId = so.Id };

        // Act
        await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<ShipmentDispatchedEvent>(), CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task CreateAsync_GeneratesShipmentNumber()
    {
        // Arrange
        SalesOrder so = await SeedPackedSalesOrderAsync();
        CreateShipmentRequest request = new() { SalesOrderId = so.Id };

        // Act
        Result<ShipmentDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.ShipmentNumber, Does.Match(@"SH-\d{8}-\d{4}"));
    }

    [Test]
    public async Task CreateAsync_NonExistentSO_ReturnsNotFound()
    {
        // Arrange
        CreateShipmentRequest request = new() { SalesOrderId = 999 };

        // Act
        Result<ShipmentDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_NOT_FOUND"));
            Assert.That(result.StatusCode, Is.EqualTo(404));
        });
    }

    [Test]
    public async Task GetByIdAsync_ExistingShipment_ReturnsShipmentWithDetails()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));
        Shipment shipment = await SeedShipmentAsync(so.Id);

        // Act
        Result<ShipmentDetailDto> result = await _sut.GetByIdAsync(shipment.Id, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Id, Is.EqualTo(shipment.Id));
            Assert.That(result.Value.TrackingEntries, Has.Count.GreaterThanOrEqualTo(1));
        });
    }

    [Test]
    public async Task UpdateStatusAsync_ValidTransition_UpdatesStatus()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));
        Shipment shipment = await SeedShipmentAsync(so.Id, status: nameof(ShipmentStatus.Dispatched));
        UpdateShipmentStatusRequest request = new() { Status = nameof(ShipmentStatus.InTransit) };

        // Act
        Result<ShipmentDetailDto> result = await _sut.UpdateStatusAsync(shipment.Id, request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Status, Is.EqualTo(nameof(ShipmentStatus.InTransit)));
        });
    }

    [Test]
    public async Task UpdateStatusAsync_InvalidTransition_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));
        Shipment shipment = await SeedShipmentAsync(so.Id, status: nameof(ShipmentStatus.Dispatched));
        UpdateShipmentStatusRequest request = new() { Status = nameof(ShipmentStatus.Returned) };

        // Act
        Result<ShipmentDetailDto> result = await _sut.UpdateStatusAsync(shipment.Id, request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INVALID_SHIPMENT_STATUS_TRANSITION"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task UpdateStatusAsync_TerminalState_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));
        Shipment shipment = await SeedShipmentAsync(so.Id, status: nameof(ShipmentStatus.Delivered));
        UpdateShipmentStatusRequest request = new() { Status = nameof(ShipmentStatus.InTransit) };

        // Act
        Result<ShipmentDetailDto> result = await _sut.UpdateStatusAsync(shipment.Id, request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INVALID_SHIPMENT_STATUS_TRANSITION"));
        });
    }

    [Test]
    public async Task UpdateStatusAsync_RecordsTrackingEntry()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));
        Shipment shipment = await SeedShipmentAsync(so.Id, status: nameof(ShipmentStatus.Dispatched));
        UpdateShipmentStatusRequest request = new() { Status = nameof(ShipmentStatus.InTransit), TrackingNumber = "TRACK123" };

        // Act
        await _sut.UpdateStatusAsync(shipment.Id, request, 1, CancellationToken.None);

        // Assert
        int trackingCount = Context.ShipmentTrackingEntries.Count(t => t.ShipmentId == shipment.Id);
        Assert.That(trackingCount, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task GetTrackingHistoryAsync_ReturnsChronologicalEntries()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));
        Shipment shipment = await SeedShipmentAsync(so.Id, status: nameof(ShipmentStatus.Dispatched));

        // Act
        Result<IReadOnlyList<ShipmentTrackingDto>> result = await _sut.GetTrackingHistoryAsync(shipment.Id, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Has.Count.GreaterThanOrEqualTo(1));
        });
    }

    [Test]
    public async Task GetTrackingHistoryAsync_NonExistentShipment_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 999;

        // Act
        Result<IReadOnlyList<ShipmentTrackingDto>> result = await _sut.GetTrackingHistoryAsync(nonExistentId, CancellationToken.None);

        // Assert
        Assert.That(result.ErrorCode, Is.EqualTo("SHIPMENT_NOT_FOUND"));
    }

    [Test]
    public async Task SearchAsync_ByStatus_ReturnsFilteredResults()
    {
        // Arrange
        SalesOrder so1 = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));
        await SeedShipmentAsync(so1.Id, status: nameof(ShipmentStatus.Dispatched));
        SalesOrder so2 = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));
        await SeedShipmentAsync(so2.Id, status: nameof(ShipmentStatus.Delivered));
        SearchShipmentsRequest request = new() { Status = nameof(ShipmentStatus.Dispatched), Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<ShipmentDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.TotalCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Seeds a fully packed SO with parcel items ready for dispatch.
    /// </summary>
    private async Task<SalesOrder> SeedPackedSalesOrderAsync()
    {
        SalesOrder so = await SeedSalesOrderAsync(
            status: nameof(SalesOrderStatus.Packed),
            productId: 100,
            orderedQuantity: 10m,
            unitPrice: 25m);

        SalesOrderLine soLine = so.Lines.First();
        soLine.PackedQuantity = 10m;
        await Context.SaveChangesAsync(CancellationToken.None);

        PickList pickList = await SeedPickListAsync(
            so.Id, soLine.Id,
            productId: 100,
            requestedQuantity: 10m,
            status: nameof(PickListStatus.Completed));

        Parcel parcel = await SeedParcelAsync(so.Id);
        await SeedParcelItemAsync(parcel.Id, pickList.Lines.First().Id, productId: 100, quantity: 10m);

        return so;
    }
}
