using MassTransit;
using Moq;
using Warehouse.Common.Enums;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Interfaces;
using Warehouse.Fulfillment.API.Services;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.Infrastructure.Correlation;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Events;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="CustomerReturnService"/> covering create, status transitions, event publishing.
/// <para>Linked to SDD-FULF-001 section 2.7.</para>
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
public sealed class CustomerReturnServiceTests : FulfillmentTestBase
{
    private Mock<IPublishEndpoint> _mockPublishEndpoint = null!;
    private Mock<ICorrelationIdAccessor> _mockCorrelationIdAccessor = null!;
    private Mock<IFulfillmentEventService> _mockEventService = null!;
    private CustomerReturnService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockCorrelationIdAccessor = new Mock<ICorrelationIdAccessor>();
        _mockEventService = new Mock<IFulfillmentEventService>();
        _sut = new CustomerReturnService(Context, Mapper, _mockPublishEndpoint.Object, _mockCorrelationIdAccessor.Object, _mockEventService.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedReturn()
    {
        // Arrange
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = 1, Reason = "Defective product",
            Lines = [new CreateCustomerReturnLineRequest { ProductId = 100, WarehouseId = 1, Quantity = 5 }]
        };

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.ReturnNumber, Does.StartWith("RMA-"));
            Assert.That(result.Value.Status, Is.EqualTo(nameof(CustomerReturnStatus.Draft)));
            Assert.That(result.Value.Lines, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task CreateAsync_WithSalesOrderId_AssociatesSOReference()
    {
        // Arrange
        SalesOrder so = await SeedSalesOrderAsync();
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = 1, SalesOrderId = so.Id, Reason = "Wrong item",
            Lines = [new CreateCustomerReturnLineRequest { ProductId = 100, WarehouseId = 1, Quantity = 3 }]
        };

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.SalesOrderId, Is.EqualTo(so.Id));
        });
    }

    [Test]
    public async Task CreateAsync_RecordsEvent()
    {
        // Arrange
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = 1, Reason = "Broken",
            Lines = [new CreateCustomerReturnLineRequest { ProductId = 100, WarehouseId = 1, Quantity = 2 }]
        };

        // Act
        await _sut.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        _mockEventService.Verify(
            e => e.RecordEventAsync("CustomerReturnCreated", "CustomerReturn", It.IsAny<int>(), 1, null, CancellationToken.None, null, null),
            Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 999;

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("RETURN_NOT_FOUND"));
            Assert.That(result.StatusCode, Is.EqualTo(404));
        });
    }

    [Test]
    public async Task ConfirmAsync_DraftReturn_TransitionsToConfirmed()
    {
        // Arrange
        CustomerReturn cr = await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Draft));

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.ConfirmAsync(cr.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Status, Is.EqualTo(nameof(CustomerReturnStatus.Confirmed)));
        });
    }

    [Test]
    public async Task ConfirmAsync_DraftReturn_RecordsConfirmationTimestamp()
    {
        // Arrange
        CustomerReturn cr = await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Draft));

        // Act
        await _sut.ConfirmAsync(cr.Id, 42, CancellationToken.None);

        // Assert
        await Context.Entry(cr).ReloadAsync(CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(cr.ConfirmedAtUtc, Is.Not.Null);
            Assert.That(cr.ConfirmedByUserId, Is.EqualTo(42));
        });
    }

    [Test]
    public async Task ConfirmAsync_AlreadyConfirmed_ReturnsConflictError()
    {
        // Arrange
        CustomerReturn cr = await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Confirmed));

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.ConfirmAsync(cr.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("RETURN_ALREADY_CONFIRMED"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task ConfirmAsync_ReturnWithNoLines_ReturnsError()
    {
        // Arrange
        CustomerReturn cr = await SeedEmptyCustomerReturnAsync(status: nameof(CustomerReturnStatus.Draft));

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.ConfirmAsync(cr.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("RETURN_MUST_HAVE_LINES"));
        });
    }

    [Test]
    public async Task ConfirmAsync_NonDraftReturn_ReturnsConflictError()
    {
        // Arrange
        CustomerReturn cr = await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Received));

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.ConfirmAsync(cr.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INVALID_RETURN_STATUS_TRANSITION"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task ReceiveAsync_ConfirmedReturn_TransitionsToReceived()
    {
        // Arrange
        CustomerReturn cr = await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Confirmed));

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.ReceiveAsync(cr.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Status, Is.EqualTo(nameof(CustomerReturnStatus.Received)));
        });
    }

    [Test]
    public async Task ReceiveAsync_ConfirmedReturn_PublishesCustomerReturnReceivedEvent()
    {
        // Arrange
        CustomerReturn cr = await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Confirmed));

        // Act
        await _sut.ReceiveAsync(cr.Id, 1, CancellationToken.None);

        // Assert
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<CustomerReturnReceivedEvent>(), CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task ReceiveAsync_NonConfirmedReturn_ReturnsConflictError()
    {
        // Arrange
        CustomerReturn cr = await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Draft));

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.ReceiveAsync(cr.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("RETURN_NOT_RECEIVABLE"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task ReceiveAsync_AlreadyReceived_ReturnsConflictError()
    {
        // Arrange
        CustomerReturn cr = await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Received));

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.ReceiveAsync(cr.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("RETURN_ALREADY_RECEIVED"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task CloseAsync_ReceivedReturn_TransitionsToClosed()
    {
        // Arrange
        CustomerReturn cr = await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Received));

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.CloseAsync(cr.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Status, Is.EqualTo(nameof(CustomerReturnStatus.Closed)));
        });
    }

    [Test]
    public async Task CloseAsync_NonReceivedReturn_ReturnsConflictError()
    {
        // Arrange
        CustomerReturn cr = await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Confirmed));

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.CloseAsync(cr.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("RETURN_NOT_CLOSEABLE"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task CancelAsync_DraftReturn_TransitionsToCancelled()
    {
        // Arrange
        CustomerReturn cr = await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Draft));

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.CancelAsync(cr.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Status, Is.EqualTo(nameof(CustomerReturnStatus.Cancelled)));
        });
    }

    [Test]
    public async Task CancelAsync_ConfirmedReturn_TransitionsToCancelled()
    {
        // Arrange
        CustomerReturn cr = await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Confirmed));

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.CancelAsync(cr.Id, 1, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.Status, Is.EqualTo(nameof(CustomerReturnStatus.Cancelled)));
    }

    [Test]
    public async Task CancelAsync_ReceivedReturn_ReturnsConflictError()
    {
        // Arrange
        CustomerReturn cr = await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Received));

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.CancelAsync(cr.Id, 1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INVALID_RETURN_STATUS_TRANSITION"));
            Assert.That(result.StatusCode, Is.EqualTo(409));
        });
    }

    [Test]
    public async Task SearchAsync_ByCustomerId_ReturnsFilteredResults()
    {
        // Arrange
        await SeedCustomerReturnAsync(customerId: 1);
        await SeedCustomerReturnAsync(customerId: 2);
        SearchCustomerReturnsRequest request = new() { CustomerId = 1, Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<CustomerReturnDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.TotalCount, Is.EqualTo(1));
    }

    [Test]
    public async Task SearchAsync_ByStatus_ReturnsFilteredResults()
    {
        // Arrange
        await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Draft));
        await SeedCustomerReturnAsync(status: nameof(CustomerReturnStatus.Confirmed));
        SearchCustomerReturnsRequest request = new() { Status = nameof(CustomerReturnStatus.Draft), Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<CustomerReturnDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.TotalCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetByIdAsync_ExistingReturn_ReturnsReturnWithLines()
    {
        // Arrange
        CustomerReturn cr = await SeedCustomerReturnAsync(customerId: 1, productId: 100, quantity: 5m);

        // Act
        Result<CustomerReturnDetailDto> result = await _sut.GetByIdAsync(cr.Id, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Id, Is.EqualTo(cr.Id));
            Assert.That(result.Value.ReturnNumber, Is.EqualTo(cr.ReturnNumber));
            Assert.That(result.Value.Lines, Has.Count.EqualTo(1));
        });
    }
}
