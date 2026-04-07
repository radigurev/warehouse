using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Purchasing.API.Interfaces;
using Warehouse.Purchasing.API.Services;
using Warehouse.Purchasing.API.Tests.Fixtures;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for purchase order lifecycle: CRUD, lines, status transitions.
/// <para>Links to specification SDD-PURCH-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class PurchaseOrderServiceTests : PurchasingTestBase
{
    private Mock<IPurchaseEventService> _mockEventService = null!;
    private PurchaseOrderService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockEventService = new Mock<IPurchaseEventService>();
        _sut = new PurchaseOrderService(Context, Mapper, _mockEventService.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedPO()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "PO-SUPP-001").ConfigureAwait(false);
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = supplier.Id,
            DestinationWarehouseId = 1,
            Notes = "Test PO",
            Lines =
            [
                new CreatePurchaseOrderLineRequest { ProductId = 100, OrderedQuantity = 50m, UnitPrice = 10m }
            ]
        };

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(PurchaseOrderStatus.Draft));
        result.Value.Lines.Should().HaveCount(1);
        result.Value.TotalAmount.Should().Be(500m);
    }

    [Test]
    public async Task CreateAsync_NonExistentSupplier_ReturnsNotFound()
    {
        // Arrange
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = 9999,
            DestinationWarehouseId = 1,
            Lines =
            [
                new CreatePurchaseOrderLineRequest { ProductId = 100, OrderedQuantity = 10m, UnitPrice = 5m }
            ]
        };

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SUPPLIER_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task CreateAsync_InactiveSupplier_ReturnsConflict()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "INACT-001", isActive: false).ConfigureAwait(false);
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = supplier.Id,
            DestinationWarehouseId = 1,
            Lines =
            [
                new CreatePurchaseOrderLineRequest { ProductId = 100, OrderedQuantity = 10m, UnitPrice = 5m }
            ]
        };

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SUPPLIER_INACTIVE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_GeneratesPONumber()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "PON-SUPP-001").ConfigureAwait(false);
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = supplier.Id,
            DestinationWarehouseId = 1,
            Lines =
            [
                new CreatePurchaseOrderLineRequest { ProductId = 100, OrderedQuantity = 10m, UnitPrice = 5m }
            ]
        };

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderNumber.Should().StartWith($"PO-{DateTime.UtcNow:yyyyMMdd}-");
    }

    [Test]
    public async Task UpdateHeaderAsync_DraftPO_ReturnsUpdatedPO()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "UH-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id).ConfigureAwait(false);
        UpdatePurchaseOrderRequest request = new()
        {
            DestinationWarehouseId = 2,
            ExpectedDeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Notes = "Updated notes"
        };

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.UpdateHeaderAsync(po.Id, request, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.DestinationWarehouseId.Should().Be(2);
        result.Value.Notes.Should().Be("Updated notes");
    }

    [Test]
    public async Task UpdateHeaderAsync_ConfirmedPO_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "UHC-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        UpdatePurchaseOrderRequest request = new() { DestinationWarehouseId = 2 };

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.UpdateHeaderAsync(po.Id, request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PO_NOT_EDITABLE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task AddLineAsync_DraftPO_ReturnsCreatedLine()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "AL-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id).ConfigureAwait(false);
        CreatePurchaseOrderLineRequest lineRequest = new()
        {
            ProductId = 200,
            OrderedQuantity = 20m,
            UnitPrice = 15m,
            Notes = "New line"
        };

        // Act
        Result<PurchaseOrderLineDto> result = await _sut.AddLineAsync(po.Id, lineRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductId.Should().Be(200);
        result.Value.OrderedQuantity.Should().Be(20m);
    }

    [Test]
    public async Task AddLineAsync_ConfirmedPO_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "ALC-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        CreatePurchaseOrderLineRequest lineRequest = new() { ProductId = 200, OrderedQuantity = 10m, UnitPrice = 5m };

        // Act
        Result<PurchaseOrderLineDto> result = await _sut.AddLineAsync(po.Id, lineRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PO_NOT_EDITABLE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task AddLineAsync_DuplicateProduct_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "ALD-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, productId: 100).ConfigureAwait(false);
        CreatePurchaseOrderLineRequest lineRequest = new() { ProductId = 100, OrderedQuantity = 10m, UnitPrice = 5m };

        // Act
        Result<PurchaseOrderLineDto> result = await _sut.AddLineAsync(po.Id, lineRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_PO_LINE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task UpdateLineAsync_DraftPO_ReturnsUpdatedLine()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "UL-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id).ConfigureAwait(false);
        PurchaseOrderLine line = po.Lines.First();
        UpdatePurchaseOrderLineRequest request = new() { OrderedQuantity = 75m, UnitPrice = 12m, Notes = "Updated line" };

        // Act
        Result<PurchaseOrderLineDto> result = await _sut.UpdateLineAsync(po.Id, line.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderedQuantity.Should().Be(75m);
        result.Value.UnitPrice.Should().Be(12m);
        result.Value.LineTotal.Should().Be(900m);
    }

    [Test]
    public async Task RemoveLineAsync_DraftPO_RemovesSuccessfully()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "RL-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id).ConfigureAwait(false);
        CreatePurchaseOrderLineRequest newLine = new() { ProductId = 300, OrderedQuantity = 5m, UnitPrice = 8m };
        await _sut.AddLineAsync(po.Id, newLine, CancellationToken.None).ConfigureAwait(false);

        PurchaseOrder? poReloaded = await Context.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == po.Id).ConfigureAwait(false);
        PurchaseOrderLine lineToRemove = poReloaded!.Lines.First(l => l.ProductId == 300);

        // Act
        Result result = await _sut.RemoveLineAsync(po.Id, lineToRemove.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task RemoveLineAsync_LastLine_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "RLL-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id).ConfigureAwait(false);
        PurchaseOrderLine singleLine = po.Lines.First();

        // Act
        Result result = await _sut.RemoveLineAsync(po.Id, singleLine.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PO_MUST_HAVE_LINES");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task GetByIdAsync_ExistingPO_ReturnsPOWithLinesAndProgress()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "GBI-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id).ConfigureAwait(false);

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.GetByIdAsync(po.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().HaveCount(1);
        result.Value.SupplierName.Should().Be("Test Supplier");
    }

    [Test]
    public async Task SearchAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "SF-SUPP").ConfigureAwait(false);
        await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Draft)).ConfigureAwait(false);
        await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        SearchPurchaseOrdersRequest request = new() { Status = nameof(PurchaseOrderStatus.Draft) };

        // Act
        Result<PaginatedResponse<PurchaseOrderDto>> result = await _sut.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
    }

    [Test]
    public async Task ConfirmAsync_DraftPO_TransitionsToConfirmed()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "CONF-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id).ConfigureAwait(false);

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.ConfirmAsync(po.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(PurchaseOrderStatus.Confirmed));
        result.Value.ConfirmedAtUtc.Should().NotBeNull();
    }

    [Test]
    public async Task ConfirmAsync_NonDraftPO_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "CONFND-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.ConfirmAsync(po.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PO_STATUS_TRANSITION");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task ConfirmAsync_POWithNoLines_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "CONFNL-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedEmptyPurchaseOrderAsync(supplier.Id).ConfigureAwait(false);

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.ConfirmAsync(po.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PO_MUST_HAVE_LINES");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CancelAsync_DraftPO_TransitionsToCancelled()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "CAND-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id).ConfigureAwait(false);

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.CancelAsync(po.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(PurchaseOrderStatus.Cancelled));
    }

    [Test]
    public async Task CancelAsync_ConfirmedPOWithNoReceipts_TransitionsToCancelled()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "CANC-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.CancelAsync(po.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(PurchaseOrderStatus.Cancelled));
    }

    [Test]
    public async Task CancelAsync_PartiallyReceivedPO_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "CANPR-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.PartiallyReceived)).ConfigureAwait(false);

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.CancelAsync(po.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PO_STATUS_TRANSITION");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CancelAsync_POWithReceipts_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "CANRCPT-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Confirmed)).ConfigureAwait(false);
        PurchaseOrderLine poLine = po.Lines.First();
        await SeedGoodsReceiptAsync(po.Id, poLine.Id).ConfigureAwait(false);

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.CancelAsync(po.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PO_HAS_RECEIPTS");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CloseAsync_PartiallyReceivedPO_TransitionsToClosed()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "CLPR-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.PartiallyReceived)).ConfigureAwait(false);

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.CloseAsync(po.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(PurchaseOrderStatus.Closed));
        result.Value.ClosedAtUtc.Should().NotBeNull();
    }

    [Test]
    public async Task CloseAsync_ReceivedPO_TransitionsToClosed()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "CLR-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Received)).ConfigureAwait(false);

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.CloseAsync(po.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(PurchaseOrderStatus.Closed));
    }

    [Test]
    public async Task CloseAsync_DraftPO_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "CLD-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id).ConfigureAwait(false);

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.CloseAsync(po.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PO_STATUS_TRANSITION");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CloseAsync_AlreadyClosedPO_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "CLAC-SUPP").ConfigureAwait(false);
        PurchaseOrder po = await SeedPurchaseOrderAsync(supplier.Id, status: nameof(PurchaseOrderStatus.Closed)).ConfigureAwait(false);

        // Act
        Result<PurchaseOrderDetailDto> result = await _sut.CloseAsync(po.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PO_ALREADY_CLOSED");
        result.StatusCode.Should().Be(409);
    }
}
