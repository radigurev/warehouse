using Moq;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Fulfillment.API.Services;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="SalesOrderService"/> covering CRUD, line management, and status transitions.
/// <para>Linked to SDD-FULF-001 sections 2.1.1 through 2.1.8.</para>
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
public sealed class SalesOrderServiceTests : FulfillmentTestBase
{
    private Mock<IFulfillmentEventService> _mockEventService = null!;
    private SalesOrderService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockEventService = new Mock<IFulfillmentEventService>();
        _sut = new SalesOrderService(Context, Mapper, _mockEventService.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedSO()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1, WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St", ShippingCity = "Springfield",
            ShippingPostalCode = "62704", ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 100, OrderedQuantity = 10, UnitPrice = 25 }]
        };

        // Act
        Result<SalesOrderDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.Status, Is.EqualTo(nameof(SalesOrderStatus.Draft)));
            Assert.That(result.Value.OrderNumber, Does.StartWith("SO-"));
            Assert.That(result.Value.TotalAmount, Is.EqualTo(250m));
            Assert.That(result.Value.Lines, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task CreateAsync_GeneratesSONumber()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1, WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St", ShippingCity = "Springfield",
            ShippingPostalCode = "62704", ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 100, OrderedQuantity = 5, UnitPrice = 10 }]
        };

        // Act
        Result<SalesOrderDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.OrderNumber, Does.Match(@"SO-\d{8}-\d{4}"));
    }

    [Test]
    public async Task CreateAsync_CalculatesTotalAmount()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1, WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St", ShippingCity = "Springfield",
            ShippingPostalCode = "62704", ShippingCountryCode = "US",
            Lines =
            [
                new CreateSalesOrderLineRequest { ProductId = 100, OrderedQuantity = 10, UnitPrice = 25 },
                new CreateSalesOrderLineRequest { ProductId = 200, OrderedQuantity = 5, UnitPrice = 50 }
            ]
        };

        // Act
        Result<SalesOrderDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.TotalAmount, Is.EqualTo(500m));
    }

    [Test]
    public async Task CreateAsync_RecordsEvent()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1, WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St", ShippingCity = "Springfield",
            ShippingPostalCode = "62704", ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 100, OrderedQuantity = 5, UnitPrice = 10 }]
        };

        // Act
        await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        _mockEventService.Verify(
            e => e.RecordEventAsync("SalesOrderCreated", "SalesOrder", It.IsAny<int>(), 1, null, CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_ExistingSO_ReturnsSOWithLines()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync();

        // Act
        Result<SalesOrderDetailDto> result = await _sut.GetByIdAsync(so.Id, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Id, Is.EqualTo(so.Id));
            Assert.That(result.Value.Lines, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 999;

        // Act
        Result<SalesOrderDetailDto> result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_NOT_FOUND"));
            Assert.That(result.StatusCode, Is.EqualTo(404));
        });
    }

    [Test]
    public async Task UpdateHeaderAsync_DraftSO_ReturnsUpdatedSO()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));
        UpdateSalesOrderRequest request = new()
        {
            WarehouseId = 2, ShippingStreetLine1 = "456 Oak Ave",
            ShippingCity = "Portland", ShippingPostalCode = "97201", ShippingCountryCode = "US"
        };

        // Act
        Result<SalesOrderDetailDto> result = await _sut.UpdateHeaderAsync(so.Id, request, 2, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.WarehouseId, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task UpdateHeaderAsync_ConfirmedSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Confirmed));
        UpdateSalesOrderRequest request = new()
        {
            WarehouseId = 2, ShippingStreetLine1 = "456 Oak Ave",
            ShippingCity = "Portland", ShippingPostalCode = "97201", ShippingCountryCode = "US"
        };

        // Act
        Result<SalesOrderDetailDto> result = await _sut.UpdateHeaderAsync(so.Id, request, 2, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_NOT_EDITABLE"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task AddLineAsync_DraftSO_ReturnsCreatedLine()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));
        CreateSalesOrderLineRequest lineRequest = new() { ProductId = 200, OrderedQuantity = 5, UnitPrice = 30 };

        // Act
        Result<SalesOrderLineDto> result = await _sut.AddLineAsync(so.Id, lineRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.ProductId, Is.EqualTo(200));
            Assert.That(result.Value.OrderedQuantity, Is.EqualTo(5));
            Assert.That(result.Value.LineTotal, Is.EqualTo(150m));
        });
    }

    [Test]
    public async Task AddLineAsync_ConfirmedSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Confirmed));
        CreateSalesOrderLineRequest lineRequest = new() { ProductId = 200, OrderedQuantity = 5, UnitPrice = 30 };

        // Act
        Result<SalesOrderLineDto> result = await _sut.AddLineAsync(so.Id, lineRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_NOT_EDITABLE"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task AddLineAsync_DuplicateProduct_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft), productId: 100);
        CreateSalesOrderLineRequest lineRequest = new() { ProductId = 100, OrderedQuantity = 5, UnitPrice = 30 };

        // Act
        Result<SalesOrderLineDto> result = await _sut.AddLineAsync(so.Id, lineRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("DUPLICATE_SO_LINE"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task AddLineAsync_RecalculatesTotalAmount()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft), orderedQuantity: 10, unitPrice: 25);
        decimal originalTotal = so.TotalAmount;
        CreateSalesOrderLineRequest lineRequest = new() { ProductId = 200, OrderedQuantity = 5, UnitPrice = 50 };

        // Act
        await _sut.AddLineAsync(so.Id, lineRequest, CancellationToken.None);

        // Assert
        await Context.Entry(so).ReloadAsync(CancellationToken.None);
        Assert.That(so.TotalAmount, Is.GreaterThan(originalTotal));
    }

    [Test]
    public async Task UpdateLineAsync_DraftSO_ReturnsUpdatedLine()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));
        int lineId = so.Lines.First().Id;
        UpdateSalesOrderLineRequest request = new() { OrderedQuantity = 20, UnitPrice = 30 };

        // Act
        Result<SalesOrderLineDto> result = await _sut.UpdateLineAsync(so.Id, lineId, request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.OrderedQuantity, Is.EqualTo(20));
            Assert.That(result.Value.UnitPrice, Is.EqualTo(30));
            Assert.That(result.Value.LineTotal, Is.EqualTo(600m));
        });
    }

    [Test]
    public async Task UpdateLineAsync_RecalculatesTotalAmount()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft), orderedQuantity: 10, unitPrice: 25);
        int lineId = so.Lines.First().Id;
        UpdateSalesOrderLineRequest request = new() { OrderedQuantity = 20, UnitPrice = 10 };

        // Act
        await _sut.UpdateLineAsync(so.Id, lineId, request, CancellationToken.None);

        // Assert
        await Context.Entry(so).ReloadAsync(CancellationToken.None);
        Assert.That(so.TotalAmount, Is.EqualTo(200m));
    }

    [Test]
    public async Task RemoveLineAsync_DraftSO_RemovesSuccessfully()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));
        CreateSalesOrderLineRequest addReq = new() { ProductId = 200, OrderedQuantity = 5, UnitPrice = 30 };
        await _sut.AddLineAsync(so.Id, addReq, CancellationToken.None);
        int firstLineId = so.Lines.First().Id;

        // Act
        Result result = await _sut.RemoveLineAsync(so.Id, firstLineId, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task RemoveLineAsync_LastLine_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));
        int lineId = so.Lines.First().Id;

        // Act
        Result result = await _sut.RemoveLineAsync(so.Id, lineId, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_MUST_HAVE_LINES"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task RemoveLineAsync_NonDraftSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Confirmed));
        int lineId = so.Lines.First().Id;

        // Act
        Result result = await _sut.RemoveLineAsync(so.Id, lineId, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_NOT_EDITABLE"));
        });
    }

    [Test]
    public async Task ConfirmAsync_DraftSO_TransitionsToConfirmed()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));

        // Act
        Result<SalesOrderDetailDto> result = await _sut.ConfirmAsync(so.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Status, Is.EqualTo(nameof(SalesOrderStatus.Confirmed)));
        });
    }

    [Test]
    public async Task ConfirmAsync_DraftSO_RecordsTimestampsAndUser()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));

        // Act
        await _sut.ConfirmAsync(so.Id, 42, CancellationToken.None);

        // Assert
        await Context.Entry(so).ReloadAsync(CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(so.ConfirmedAtUtc, Is.Not.Null);
            Assert.That(so.ConfirmedByUserId, Is.EqualTo(42));
            Assert.That(so.ModifiedAtUtc, Is.Not.Null);
            Assert.That(so.ModifiedByUserId, Is.EqualTo(42));
        });
    }

    [Test]
    public async Task ConfirmAsync_NonDraftSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Confirmed));

        // Act
        Result<SalesOrderDetailDto> result = await _sut.ConfirmAsync(so.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INVALID_SO_STATUS_TRANSITION"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task ConfirmAsync_SOWithNoLines_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedEmptySalesOrderAsync(status: nameof(SalesOrderStatus.Draft));

        // Act
        Result<SalesOrderDetailDto> result = await _sut.ConfirmAsync(so.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_MUST_HAVE_LINES"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task CancelAsync_DraftSO_TransitionsToCancelled()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));

        // Act
        Result<SalesOrderDetailDto> result = await _sut.CancelAsync(so.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Status, Is.EqualTo(nameof(SalesOrderStatus.Cancelled)));
        });
    }

    [Test]
    public async Task CancelAsync_ConfirmedSOWithNoPickLists_TransitionsToCancelled()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Confirmed));

        // Act
        Result<SalesOrderDetailDto> result = await _sut.CancelAsync(so.Id, 1, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Status, Is.EqualTo(nameof(SalesOrderStatus.Cancelled)));
    }

    [Test]
    public async Task CancelAsync_SOWithPickLists_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Confirmed));
        await SeedPickListAsync(so.Id, so.Lines.First().Id);

        // Act
        Result<SalesOrderDetailDto> result = await _sut.CancelAsync(so.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_HAS_PICK_LISTS"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task CancelAsync_PickingSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Picking));

        // Act
        Result<SalesOrderDetailDto> result = await _sut.CancelAsync(so.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INVALID_SO_STATUS_TRANSITION"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task CancelAsync_ShippedSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));

        // Act
        Result<SalesOrderDetailDto> result = await _sut.CancelAsync(so.Id, 1, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("INVALID_SO_STATUS_TRANSITION"));
    }

    [Test]
    public async Task CompleteAsync_ShippedSO_TransitionsToCompleted()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));

        // Act
        Result<SalesOrderDetailDto> result = await _sut.CompleteAsync(so.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Status, Is.EqualTo(nameof(SalesOrderStatus.Completed)));
        });
    }

    [Test]
    public async Task CompleteAsync_ShippedSO_RecordsCompletionTimestamps()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Shipped));

        // Act
        await _sut.CompleteAsync(so.Id, 42, CancellationToken.None);

        // Assert
        await Context.Entry(so).ReloadAsync(CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(so.CompletedAtUtc, Is.Not.Null);
            Assert.That(so.CompletedByUserId, Is.EqualTo(42));
        });
    }

    [Test]
    public async Task CompleteAsync_NonShippedSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Packed));

        // Act
        Result<SalesOrderDetailDto> result = await _sut.CompleteAsync(so.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INVALID_SO_STATUS_TRANSITION"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task CompleteAsync_AlreadyCompletedSO_ReturnsConflictError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Completed));

        // Act
        Result<SalesOrderDetailDto> result = await _sut.CompleteAsync(so.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SO_ALREADY_COMPLETED"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task SearchAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft), customerId: 1);
        await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Confirmed), customerId: 2);
        SearchSalesOrdersRequest request = new() { CustomerId = 1, Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<SalesOrderDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.TotalCount, Is.EqualTo(1));
            Assert.That(result.Value.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task SearchAsync_ByStatus_ReturnsFilteredResults()
    {
        // Arrange
        await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));
        await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Confirmed));
        SearchSalesOrdersRequest request = new() { Status = nameof(SalesOrderStatus.Draft), Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<SalesOrderDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.TotalCount, Is.EqualTo(1));
    }
}
