using MassTransit;
using Moq;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Fulfillment.API.Services;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="PickListService"/> covering generation, pick confirmation, cancellation, and event publishing.
/// <para>Linked to SDD-FULF-001 section 2.2.</para>
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
public sealed class PickListServiceTests : FulfillmentTestBase
{
    private Mock<IPublishEndpoint> _mockPublishEndpoint = null!;
    private Mock<IFulfillmentEventService> _mockEventService = null!;
    private PickListService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockEventService = new Mock<IFulfillmentEventService>();
        _sut = new PickListService(Context, Mapper, _mockPublishEndpoint.Object, _mockEventService.Object);
    }

    [Test]
    public async Task GenerateAsync_ConfirmedSO_ReturnsCreatedPickList()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Confirmed));
        GeneratePickListRequest request = new() { SalesOrderId = so.Id };

        // Act
        Result<PickListDetailDto> result = await _sut.GenerateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Status, Is.EqualTo(nameof(PickListStatus.Pending)));
            Assert.That(result.Value.Lines, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task GenerateAsync_GeneratesPickListNumber()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Confirmed));
        GeneratePickListRequest request = new() { SalesOrderId = so.Id };

        // Act
        Result<PickListDetailDto> result = await _sut.GenerateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.PickListNumber, Does.Match(@"PL-\d{8}-\d{4}"));
    }

    [Test]
    public async Task GenerateAsync_TransitionsSOToPicking()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Confirmed));
        GeneratePickListRequest request = new() { SalesOrderId = so.Id };

        // Act
        await _sut.GenerateAsync(request, 1, CancellationToken.None);

        // Assert
        await Context.Entry(so).ReloadAsync(CancellationToken.None);
        Assert.That(so.Status, Is.EqualTo(nameof(SalesOrderStatus.Picking)));
    }

    [Test]
    public async Task GenerateAsync_PublishesStockReservationRequestedEvent()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Confirmed));
        GeneratePickListRequest request = new() { SalesOrderId = so.Id };

        // Act
        await _sut.GenerateAsync(request, 1, CancellationToken.None);

        // Assert
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<StockReservationRequestedEvent>(), CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GenerateAsync_DraftSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));
        GeneratePickListRequest request = new() { SalesOrderId = so.Id };

        // Act
        Result<PickListDetailDto> result = await _sut.GenerateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_NOT_PICKABLE"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task GenerateAsync_CancelledSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Cancelled));
        GeneratePickListRequest request = new() { SalesOrderId = so.Id };

        // Act
        Result<PickListDetailDto> result = await _sut.GenerateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_NOT_PICKABLE"));
        });
    }

    [Test]
    public async Task GenerateAsync_FullyAllocatedSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        SalesOrderLine soLine = so.Lines.First();
        await SeedPickListAsync(so.Id, soLine.Id, productId: soLine.ProductId, requestedQuantity: soLine.OrderedQuantity);
        GeneratePickListRequest request = new() { SalesOrderId = so.Id };

        // Act
        Result<PickListDetailDto> result = await _sut.GenerateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_FULLY_ALLOCATED"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task GenerateAsync_NonExistentSO_ReturnsNotFound()
    {
        // Arrange
        GeneratePickListRequest request = new() { SalesOrderId = 999 };

        // Act
        Result<PickListDetailDto> result = await _sut.GenerateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.That(result.ErrorCode, Is.EqualTo("SO_NOT_FOUND"));
    }

    [Test]
    public async Task ConfirmPickAsync_ValidRequest_SetsLineAsPicked()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        PickList pl = await SeedPickListAsync(so.Id, so.Lines.First().Id, requestedQuantity: 10m);
        int lineId = pl.Lines.First().Id;
        ConfirmPickRequest request = new() { ActualQuantity = 10m };

        // Act
        Result<PickListLineDto> result = await _sut.ConfirmPickAsync(pl.Id, lineId, request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.ActualQuantity, Is.EqualTo(10m));
            Assert.That(result.Value.Status, Is.EqualTo(nameof(PickListStatus.Completed)));
        });
    }

    [Test]
    public async Task ConfirmPickAsync_AlreadyPicked_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        PickList pl = await SeedPickListAsync(so.Id, so.Lines.First().Id, requestedQuantity: 10m, status: nameof(PickListStatus.Completed));
        int lineId = pl.Lines.First().Id;
        ConfirmPickRequest request = new() { ActualQuantity = 10m };

        // Act
        Result<PickListLineDto> result = await _sut.ConfirmPickAsync(pl.Id, lineId, request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("LINE_ALREADY_PICKED"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task ConfirmPickAsync_OverPick_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        PickList pl = await SeedPickListAsync(so.Id, so.Lines.First().Id, requestedQuantity: 10m);
        int lineId = pl.Lines.First().Id;
        ConfirmPickRequest request = new() { ActualQuantity = 15m };

        // Act
        Result<PickListLineDto> result = await _sut.ConfirmPickAsync(pl.Id, lineId, request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("OVER_PICK"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task ConfirmPickAsync_ShortPick_ReleasesExcessReservation()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        PickList pl = await SeedPickListAsync(so.Id, so.Lines.First().Id, requestedQuantity: 10m);
        int lineId = pl.Lines.First().Id;
        ConfirmPickRequest request = new() { ActualQuantity = 7m };

        // Act
        await _sut.ConfirmPickAsync(pl.Id, lineId, request, 1, CancellationToken.None);

        // Assert
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<StockReservationReleasedEvent>(), CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task ConfirmPickAsync_AllLinesPicked_TransitionsPickListToCompleted()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        PickList pl = await SeedPickListAsync(so.Id, so.Lines.First().Id, requestedQuantity: 10m);
        int lineId = pl.Lines.First().Id;
        ConfirmPickRequest request = new() { ActualQuantity = 10m };

        // Act
        await _sut.ConfirmPickAsync(pl.Id, lineId, request, 1, CancellationToken.None);

        // Assert
        await Context.Entry(pl).ReloadAsync(CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(pl.Status, Is.EqualTo(nameof(PickListStatus.Completed)));
            Assert.That(pl.CompletedAtUtc, Is.Not.Null);
        });
    }

    [Test]
    public async Task ConfirmPickAsync_NonExistentPickList_ReturnsNotFound()
    {
        // Arrange
        ConfirmPickRequest request = new() { ActualQuantity = 10m };

        // Act
        Result<PickListLineDto> result = await _sut.ConfirmPickAsync(999, 1, request, 1, CancellationToken.None);

        // Assert
        Assert.That(result.ErrorCode, Is.EqualTo("PICK_LIST_NOT_FOUND"));
    }

    [Test]
    public async Task CancelAsync_PendingPickList_SetsCancelledStatus()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        PickList pl = await SeedPickListAsync(so.Id, so.Lines.First().Id);

        // Act
        Result<PickListDetailDto> result = await _sut.CancelAsync(pl.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Status, Is.EqualTo(nameof(PickListStatus.Cancelled)));
        });
    }

    [Test]
    public async Task CancelAsync_PendingPickList_ReleasesReservations()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        PickList pl = await SeedPickListAsync(so.Id, so.Lines.First().Id);

        // Act
        await _sut.CancelAsync(pl.Id, 1, CancellationToken.None);

        // Assert
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<StockReservationReleasedEvent>(), CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task CancelAsync_CompletedPickList_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        PickList pl = await SeedPickListAsync(so.Id, so.Lines.First().Id, status: nameof(PickListStatus.Completed));

        // Act
        Result<PickListDetailDto> result = await _sut.CancelAsync(pl.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("PICK_LIST_ALREADY_COMPLETED"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task GetByIdAsync_ExistingPickList_ReturnsPickListWithLines()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        PickList pl = await SeedPickListAsync(so.Id, so.Lines.First().Id);

        // Act
        Result<PickListDetailDto> result = await _sut.GetByIdAsync(pl.Id, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Id, Is.EqualTo(pl.Id));
            Assert.That(result.Value.Lines, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task SearchAsync_BySalesOrderId_ReturnsFilteredResults()
    {
        // Arrange
        SalesOrder so1 = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        await SeedPickListAsync(so1.Id, so1.Lines.First().Id);
        SalesOrder so2 = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));
        await SeedPickListAsync(so2.Id, so2.Lines.First().Id);
        SearchPickListsRequest request = new() { SalesOrderId = so1.Id, Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<PickListDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.TotalCount, Is.EqualTo(1));
    }
}
