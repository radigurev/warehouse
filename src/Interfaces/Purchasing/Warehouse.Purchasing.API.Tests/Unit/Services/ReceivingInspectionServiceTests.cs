using FluentAssertions;
using MassTransit;
using Moq;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.Purchasing.API.Services;
using Warehouse.Purchasing.API.Tests.Fixtures;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for receiving inspection operations: inspect lines and resolve quarantine.
/// <para>Links to specification SDD-PURCH-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class ReceivingInspectionServiceTests : PurchasingTestBase
{
    private Mock<IPublishEndpoint> _mockPublishEndpoint = null!;
    private Mock<IPurchaseEventService> _mockEventService = null!;
    private ReceivingInspectionService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockEventService = new Mock<IPurchaseEventService>();
        _sut = new ReceivingInspectionService(Context, Mapper, _mockPublishEndpoint.Object, _mockEventService.Object);
    }

    [Test]
    public async Task InspectAsync_PendingLine_SetsStatusToAccepted()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "RI-ACC-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        GoodsReceipt receipt = await SeedGoodsReceiptAsync(po.Id, poLine.Id, inspectionStatus: nameof(InspectionStatus.Pending)).ConfigureAwait(false);
        GoodsReceiptLine grLine = receipt.Lines.First();
        InspectLineRequest request = new() { InspectionStatus = nameof(InspectionStatus.Accepted), InspectionNote = "Quality OK" };

        // Act
        Result<GoodsReceiptLineDto> result = await _sut.InspectAsync(receipt.Id, grLine.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.InspectionStatus.Should().Be(nameof(InspectionStatus.Accepted));
    }

    [Test]
    public async Task InspectAsync_PendingLine_SetsStatusToRejected()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "RI-REJ-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        GoodsReceipt receipt = await SeedGoodsReceiptAsync(po.Id, poLine.Id, inspectionStatus: nameof(InspectionStatus.Pending)).ConfigureAwait(false);
        GoodsReceiptLine grLine = receipt.Lines.First();
        InspectLineRequest request = new() { InspectionStatus = nameof(InspectionStatus.Rejected), InspectionNote = "Defective" };

        // Act
        Result<GoodsReceiptLineDto> result = await _sut.InspectAsync(receipt.Id, grLine.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.InspectionStatus.Should().Be(nameof(InspectionStatus.Rejected));
    }

    [Test]
    public async Task InspectAsync_PendingLine_SetsStatusToQuarantined()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "RI-QUA-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        GoodsReceipt receipt = await SeedGoodsReceiptAsync(po.Id, poLine.Id, inspectionStatus: nameof(InspectionStatus.Pending)).ConfigureAwait(false);
        GoodsReceiptLine grLine = receipt.Lines.First();
        InspectLineRequest request = new() { InspectionStatus = nameof(InspectionStatus.Quarantined), InspectionNote = "Needs review" };

        // Act
        Result<GoodsReceiptLineDto> result = await _sut.InspectAsync(receipt.Id, grLine.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.InspectionStatus.Should().Be(nameof(InspectionStatus.Quarantined));
    }

    [Test]
    public async Task InspectAsync_AlreadyInspected_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "RI-ALR-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        GoodsReceipt receipt = await SeedGoodsReceiptAsync(po.Id, poLine.Id, inspectionStatus: nameof(InspectionStatus.Accepted)).ConfigureAwait(false);
        GoodsReceiptLine grLine = receipt.Lines.First();
        InspectLineRequest request = new() { InspectionStatus = nameof(InspectionStatus.Accepted) };

        // Act
        Result<GoodsReceiptLineDto> result = await _sut.InspectAsync(receipt.Id, grLine.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("LINE_ALREADY_INSPECTED");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task ResolveQuarantineAsync_AcceptedResolution_PublishesEvent()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "RI-RQACC-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        GoodsReceipt receipt = await SeedGoodsReceiptAsync(po.Id, poLine.Id, inspectionStatus: nameof(InspectionStatus.Quarantined)).ConfigureAwait(false);
        GoodsReceiptLine grLine = receipt.Lines.First();
        ResolveQuarantineRequest request = new() { Resolution = nameof(InspectionStatus.Accepted), Note = "Passed second review" };

        // Act
        Result<GoodsReceiptLineDto> result = await _sut.ResolveQuarantineAsync(receipt.Id, grLine.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.InspectionStatus.Should().Be(nameof(InspectionStatus.Accepted));
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<GoodsReceiptLineAcceptedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ResolveQuarantineAsync_RejectedResolution_DoesNotPublishEvent()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "RI-RQREJ-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        GoodsReceipt receipt = await SeedGoodsReceiptAsync(po.Id, poLine.Id, inspectionStatus: nameof(InspectionStatus.Quarantined)).ConfigureAwait(false);
        GoodsReceiptLine grLine = receipt.Lines.First();
        ResolveQuarantineRequest request = new() { Resolution = nameof(InspectionStatus.Rejected), Note = "Failed second review" };

        // Act
        Result<GoodsReceiptLineDto> result = await _sut.ResolveQuarantineAsync(receipt.Id, grLine.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.InspectionStatus.Should().Be(nameof(InspectionStatus.Rejected));
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<GoodsReceiptLineAcceptedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ResolveQuarantineAsync_NonQuarantinedLine_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "RI-RQNQ-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        GoodsReceipt receipt = await SeedGoodsReceiptAsync(po.Id, poLine.Id, inspectionStatus: nameof(InspectionStatus.Accepted)).ConfigureAwait(false);
        GoodsReceiptLine grLine = receipt.Lines.First();
        ResolveQuarantineRequest request = new() { Resolution = nameof(InspectionStatus.Accepted) };

        // Act
        Result<GoodsReceiptLineDto> result = await _sut.ResolveQuarantineAsync(receipt.Id, grLine.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("LINE_NOT_QUARANTINED");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task InspectAsync_RejectedLine_DoesNotCountTowardReceivedTotal()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "RI-REJRCV-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed), orderedQuantity: 20m).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        GoodsReceipt receipt = await SeedGoodsReceiptAsync(po.Id, poLine.Id, receivedQuantity: 20m, inspectionStatus: nameof(InspectionStatus.Pending)).ConfigureAwait(false);
        GoodsReceiptLine grLine = receipt.Lines.First();
        InspectLineRequest request = new() { InspectionStatus = nameof(InspectionStatus.Rejected), InspectionNote = "Damaged beyond use" };

        // Act
        Result<GoodsReceiptLineDto> result = await _sut.InspectAsync(receipt.Id, grLine.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.InspectionStatus.Should().Be(nameof(InspectionStatus.Rejected));

        PurchaseOrderLine? reloadedPoLine = await Context.PurchaseOrderLines.FindAsync(new object[] { poLine.Id }, CancellationToken.None).ConfigureAwait(false);
        reloadedPoLine.Should().NotBeNull();
        reloadedPoLine!.ReceivedQuantity.Should().Be(0m);
    }
}
