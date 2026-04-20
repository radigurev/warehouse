using Microsoft.EntityFrameworkCore;
using Moq;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Fulfillment.API.Services;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.Infrastructure.Caching;
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
    private Mock<INomenclatureResolver> _mockNomenclatureResolver = null!;
    private Mock<IProductPriceResolver> _mockPriceResolver = null!;
    private SalesOrderService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockEventService = new Mock<IFulfillmentEventService>();
        _mockNomenclatureResolver = new Mock<INomenclatureResolver>();
        _mockPriceResolver = new Mock<IProductPriceResolver>();

        // Default: resolver returns a stable $25 USD price so pre-existing tests that do not care
        // about price resolution continue to work. Override in specific tests when exercising the
        // FULF_PRICE_NOT_FOUND path or custom catalog prices (CHG-FEAT-007 §2.3).
        _mockPriceResolver
            .Setup(r => r.ResolveAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int productId, string currencyCode, DateTime _, CancellationToken __) => new Warehouse.Fulfillment.DBModel.Models.ProductPrice
            {
                Id = 1,
                ProductId = productId,
                CurrencyCode = currencyCode,
                UnitPrice = 25m,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = 1
            });

        _sut = new SalesOrderService(Context, Mapper, _mockEventService.Object, _mockNomenclatureResolver.Object, _mockPriceResolver.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedSO()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1, CustomerAccountId = 1, CurrencyCode = "USD", WarehouseId = 1,
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
            CustomerId = 1, CustomerAccountId = 1, CurrencyCode = "USD", WarehouseId = 1,
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
            CustomerId = 1, CustomerAccountId = 1, CurrencyCode = "USD", WarehouseId = 1,
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
            CustomerId = 1, CustomerAccountId = 1, CurrencyCode = "USD", WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St", ShippingCity = "Springfield",
            ShippingPostalCode = "62704", ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 100, OrderedQuantity = 5, UnitPrice = 10 }]
        };

        // Act
        await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        _mockEventService.Verify(
            e => e.RecordEventAsync("SalesOrderCreated", "SalesOrder", It.IsAny<int>(), 1, null, CancellationToken.None, null, null),
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

    [Test]
    public async Task RemoveLineAsync_RecalculatesTotalAmount()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft), productId: 100, orderedQuantity: 10, unitPrice: 25);
        SalesOrderLine secondLine = new()
        {
            SalesOrderId = so.Id,
            ProductId = 200,
            OrderedQuantity = 5,
            UnitPrice = 50,
            LineTotal = 250m,
            PickedQuantity = 0,
            PackedQuantity = 0,
            ShippedQuantity = 0
        };
        Context.SalesOrderLines.Add(secondLine);
        so.TotalAmount = 500m;
        await Context.SaveChangesAsync(CancellationToken.None);

        int firstLineId = so.Lines.First(l => l.ProductId == 100).Id;

        // Act
        Result result = await _sut.RemoveLineAsync(so.Id, firstLineId, CancellationToken.None);

        // Assert
        await Context.Entry(so).ReloadAsync(CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(so.TotalAmount, Is.EqualTo(250m));
        });
    }

    /// <summary>CHG-FEAT-007 §2.3 — add line without UnitPrice uses the resolved catalog price.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task CreateLineAsync_NoUnitPriceProvided_UsesResolvedCatalogPrice()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));
        _mockPriceResolver
            .Setup(r => r.ResolveAsync(250, "USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductPrice
            {
                Id = 1,
                ProductId = 250,
                CurrencyCode = "USD",
                UnitPrice = 88.8800m,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = 1
            });
        CreateSalesOrderLineRequest request = new() { ProductId = 250, OrderedQuantity = 2m, UnitPrice = null };

        // Act
        Result<SalesOrderLineDto> result = await _sut.AddLineAsync(so.Id, request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.UnitPrice, Is.EqualTo(88.88m));
            Assert.That(result.Value.LineTotal, Is.EqualTo(177.76m));
        });
    }

    /// <summary>CHG-FEAT-007 §2.3 — caller-supplied UnitPrice is preserved verbatim even when catalog has a different price.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task CreateLineAsync_UnitPriceProvided_PreservesCallerOverride()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));
        _mockPriceResolver
            .Setup(r => r.ResolveAsync(260, "USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductPrice
            {
                Id = 2,
                ProductId = 260,
                CurrencyCode = "USD",
                UnitPrice = 99m,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = 1
            });
        CreateSalesOrderLineRequest request = new() { ProductId = 260, OrderedQuantity = 2m, UnitPrice = 17m };

        // Act
        Result<SalesOrderLineDto> result = await _sut.AddLineAsync(so.Id, request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.UnitPrice, Is.EqualTo(17m));
        });
    }

    /// <summary>CHG-FEAT-007 §2.3 — override does NOT bypass the catalog existence check.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task CreateLineAsync_UnitPriceProvidedButNoActivePriceExists_StillReturnsNotFoundError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));
        _mockPriceResolver
            .Setup(r => r.ResolveAsync(270, "USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductPrice?)null);
        CreateSalesOrderLineRequest request = new() { ProductId = 270, OrderedQuantity = 2m, UnitPrice = 30m };

        // Act
        Result<SalesOrderLineDto> result = await _sut.AddLineAsync(so.Id, request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("FULF_PRICE_NOT_FOUND"));
            Assert.That(result.StatusCode, Is.EqualTo(400));
        });
    }

    /// <summary>CHG-FEAT-007 §2.3 / §2.4 — no catalog entry blocks add-line with FULF_PRICE_NOT_FOUND (and nothing is persisted).</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task CreateLineAsync_NoActivePrice_ReturnsFulfPriceNotFoundError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));
        _mockPriceResolver
            .Setup(r => r.ResolveAsync(280, "USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductPrice?)null);
        CreateSalesOrderLineRequest request = new() { ProductId = 280, OrderedQuantity = 2m, UnitPrice = null };

        // Act
        Result<SalesOrderLineDto> result = await _sut.AddLineAsync(so.Id, request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("FULF_PRICE_NOT_FOUND"));
            Assert.That(result.StatusCode, Is.EqualTo(400));
        });

        int persistedCount = await Context.SalesOrderLines.CountAsync(l => l.ProductId == 280, CancellationToken.None);
        Assert.That(persistedCount, Is.Zero, "No partial line MUST be persisted (§2.4).");
    }

    /// <summary>CHG-FEAT-007 §2.3 step 1 — resolver is invoked with the currency stored directly on the SO header.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task CreateLineAsync_UsesCustomerAccountCurrency_AsLookupKey()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft));
        so.CurrencyCode = "EUR";
        await Context.SaveChangesAsync(CancellationToken.None);

        _mockPriceResolver
            .Setup(r => r.ResolveAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int pid, string cur, DateTime _, CancellationToken __) => new ProductPrice
            {
                Id = 1,
                ProductId = pid,
                CurrencyCode = cur,
                UnitPrice = 10m,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = 1
            });

        CreateSalesOrderLineRequest request = new() { ProductId = 290, OrderedQuantity = 1m, UnitPrice = null };

        // Act
        await _sut.AddLineAsync(so.Id, request, CancellationToken.None);

        // Assert
        _mockPriceResolver.Verify(
            r => r.ResolveAsync(290, "EUR", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>CHG-FEAT-007 §2.3 — update without UnitPrice re-resolves and applies the latest catalog price.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task UpdateLineAsync_NoUnitPriceProvided_RerunsResolverAndUpdatesPrice()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft), productId: 300, unitPrice: 25m);
        int lineId = so.Lines.First().Id;
        _mockPriceResolver
            .Setup(r => r.ResolveAsync(300, "USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductPrice
            {
                Id = 99,
                ProductId = 300,
                CurrencyCode = "USD",
                UnitPrice = 33m,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = 1
            });
        UpdateSalesOrderLineRequest request = new() { OrderedQuantity = 4m, UnitPrice = null };

        // Act
        Result<SalesOrderLineDto> result = await _sut.UpdateLineAsync(so.Id, lineId, request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.UnitPrice, Is.EqualTo(33m));
            Assert.That(result.Value.LineTotal, Is.EqualTo(132m));
        });
    }

    /// <summary>CHG-FEAT-007 §2.3 — update with explicit UnitPrice preserves the caller override.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task UpdateLineAsync_UnitPriceProvided_PreservesCallerOverride()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft), productId: 310, unitPrice: 25m);
        int lineId = so.Lines.First().Id;
        _mockPriceResolver
            .Setup(r => r.ResolveAsync(310, "USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductPrice
            {
                Id = 99,
                ProductId = 310,
                CurrencyCode = "USD",
                UnitPrice = 99m,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = 1
            });
        UpdateSalesOrderLineRequest request = new() { OrderedQuantity = 2m, UnitPrice = 5m };

        // Act
        Result<SalesOrderLineDto> result = await _sut.UpdateLineAsync(so.Id, lineId, request, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.UnitPrice, Is.EqualTo(5m));
    }

    /// <summary>CHG-FEAT-007 §2.3 — update path blocks when catalog lost coverage since the SO was created.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task UpdateLineAsync_NoActivePrice_ReturnsFulfPriceNotFoundError()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync(status: nameof(SalesOrderStatus.Draft), productId: 320, unitPrice: 25m);
        int lineId = so.Lines.First().Id;
        _mockPriceResolver
            .Setup(r => r.ResolveAsync(320, "USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductPrice?)null);
        UpdateSalesOrderLineRequest request = new() { OrderedQuantity = 2m, UnitPrice = null };

        // Act
        Result<SalesOrderLineDto> result = await _sut.UpdateLineAsync(so.Id, lineId, request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("FULF_PRICE_NOT_FOUND"));
            Assert.That(result.StatusCode, Is.EqualTo(400));
        });
    }

    /// <summary>CHG-FEAT-007 §2.9 — CustomerAccountId and CurrencyCode from the request are persisted on the SO header.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task CreateSalesOrderAsync_CustomerAccountIdAndCurrencyCode_PersistedOnHeader()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 77,
            CurrencyCode = "EUR",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Berlin",
            ShippingPostalCode = "10115",
            ShippingCountryCode = "DE",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 400, OrderedQuantity = 1m, UnitPrice = 10m }]
        };

        // Act
        Result<SalesOrderDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.CustomerAccountId, Is.EqualTo(77));
            Assert.That(result.Value.CurrencyCode, Is.EqualTo("EUR"));
        });
    }
}
