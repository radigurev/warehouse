using FluentAssertions;
using MassTransit;
using Moq;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Sequences;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.Purchasing.API.Services;
using Warehouse.Purchasing.API.Tests.Fixtures;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for goods receipt operations: create, complete, search, event publishing.
/// <para>Links to specification SDD-PURCH-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class GoodsReceiptServiceTests : PurchasingTestBase
{
    private Mock<IPublishEndpoint> _mockPublishEndpoint = null!;
    private Mock<IPurchaseEventService> _mockEventService = null!;
    private Mock<ISequenceGenerator> _mockSequenceGenerator = null!;
    private GoodsReceiptService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockEventService = new Mock<IPurchaseEventService>();
        _mockSequenceGenerator = new Mock<ISequenceGenerator>();
        _sut = new GoodsReceiptService(Context, Mapper, _mockPublishEndpoint.Object, _mockEventService.Object, _mockSequenceGenerator.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedReceipt()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "GR-SUPP-001").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = po.Id,
            WarehouseId = 1,
            Lines =
            [
                new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = poLine.Id, ReceivedQuantity = 10m }
            ]
        };

        // Act
        Result<GoodsReceiptDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(GoodsReceiptStatus.Open));
        result.Value.Lines.Should().HaveCount(1);
        result.Value.Lines[0].ReceivedQuantity.Should().Be(10m);
    }

    [Test]
    public async Task CreateAsync_CancelledPO_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "GRC-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Cancelled)).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = po.Id,
            WarehouseId = 1,
            Lines =
            [
                new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = poLine.Id, ReceivedQuantity = 5m }
            ]
        };

        // Act
        Result<GoodsReceiptDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PO_NOT_RECEIVABLE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_DraftPO_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "GRD-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Draft)).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = po.Id,
            WarehouseId = 1,
            Lines =
            [
                new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = poLine.Id, ReceivedQuantity = 5m }
            ]
        };

        // Act
        Result<GoodsReceiptDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PO_NOT_RECEIVABLE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_GeneratesReceiptNumber()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "GRN-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = po.Id,
            WarehouseId = 1,
            Lines =
            [
                new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = poLine.Id, ReceivedQuantity = 5m }
            ]
        };

        // Act
        Result<GoodsReceiptDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ReceiptNumber.Should().StartWith($"GR-{DateTime.UtcNow:yyyyMMdd}-");
    }

    [Test]
    public async Task CreateAsync_OverReceipt_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "GROR-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed), orderedQuantity: 10m).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = po.Id,
            WarehouseId = 1,
            Lines =
            [
                new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = poLine.Id, ReceivedQuantity = 15m }
            ]
        };

        // Act
        Result<GoodsReceiptDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("OVER_RECEIPT");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_FullyReceivedLine_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "GRFR-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.PartiallyReceived), orderedQuantity: 10m).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        poLine.ReceivedQuantity = 10m;
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = po.Id,
            WarehouseId = 1,
            Lines =
            [
                new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = poLine.Id, ReceivedQuantity = 1m }
            ]
        };

        // Act
        Result<GoodsReceiptDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("LINE_FULLY_RECEIVED");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_InvalidPOLineId_ReturnsValidationError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "GRIP-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = po.Id,
            WarehouseId = 1,
            Lines =
            [
                new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = 99999, ReceivedQuantity = 5m }
            ]
        };

        // Act
        Result<GoodsReceiptDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PO_LINE");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task CompleteAsync_AcceptedLines_PublishesGoodsReceiptCompletedEvent()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "GRCOMP-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        GoodsReceipt receipt = await SeedGoodsReceiptAsync(po.Id, poLine.Id, receivedQuantity: 10m, inspectionStatus: nameof(InspectionStatus.Accepted)).ConfigureAwait(false);

        // Act
        Result<GoodsReceiptDetailDto> result = await _sut.CompleteAsync(receipt.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(GoodsReceiptStatus.Completed));
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<GoodsReceiptCompletedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CompleteAsync_UpdatesPOStatusToPartiallyReceived()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "GRCPR-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed), orderedQuantity: 50m).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        GoodsReceipt receipt = await SeedGoodsReceiptAsync(po.Id, poLine.Id, receivedQuantity: 20m, inspectionStatus: nameof(InspectionStatus.Accepted)).ConfigureAwait(false);

        // Act
        Result<GoodsReceiptDetailDto> result = await _sut.CompleteAsync(receipt.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await Context.Entry(po).ReloadAsync(CancellationToken.None).ConfigureAwait(false);
        po.Status.Should().Be(nameof(PurchaseOrderStatus.PartiallyReceived));
    }

    [Test]
    public async Task CompleteAsync_AllLinesReceived_UpdatesPOStatusToReceived()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "GRCALL-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed), orderedQuantity: 10m).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        GoodsReceipt receipt = await SeedGoodsReceiptAsync(po.Id, poLine.Id, receivedQuantity: 10m, inspectionStatus: nameof(InspectionStatus.Accepted)).ConfigureAwait(false);

        // Act
        Result<GoodsReceiptDetailDto> result = await _sut.CompleteAsync(receipt.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await Context.Entry(po).ReloadAsync(CancellationToken.None).ConfigureAwait(false);
        po.Status.Should().Be(nameof(PurchaseOrderStatus.Received));
    }

    [Test]
    public async Task GetByIdAsync_ExistingReceipt_ReturnsReceiptWithLines()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "GRGET-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        GoodsReceipt receipt = await SeedGoodsReceiptAsync(po.Id, poLine.Id).ConfigureAwait(false);

        // Act
        Result<GoodsReceiptDetailDto> result = await _sut.GetByIdAsync(receipt.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().HaveCount(1);
    }

    [Test]
    public async Task SearchAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "GRSRCH-SUPP").ConfigureAwait(false);
        PurchaseOrder po1 = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        PurchaseOrder po2 = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        PurchaseOrderLine poLine1 = po1.Lines.First();
        PurchaseOrderLine poLine2 = po2.Lines.First();
        await SeedGoodsReceiptAsync(po1.Id, poLine1.Id, warehouseId: 1).ConfigureAwait(false);
        await SeedGoodsReceiptAsync(po2.Id, poLine2.Id, warehouseId: 2).ConfigureAwait(false);
        SearchGoodsReceiptsRequest request = new() { WarehouseId = 1 };

        // Act
        Result<PaginatedResponse<GoodsReceiptDto>> result = await _sut.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
    }
}
