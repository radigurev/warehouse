using FluentAssertions;
using MassTransit;
using Moq;
using Warehouse.Common.Enums;
using Warehouse.Infrastructure.Correlation;
using Warehouse.Common.Models;
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
/// Unit tests for supplier return operations: create, confirm, cancel, search, event publishing.
/// <para>Links to specification SDD-PURCH-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class SupplierReturnServiceTests : PurchasingTestBase
{
    private Mock<IPublishEndpoint> _mockPublishEndpoint = null!;
    private Mock<ICorrelationIdAccessor> _mockCorrelationIdAccessor = null!;
    private Mock<IPurchaseEventService> _mockEventService = null!;
    private SupplierReturnService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockCorrelationIdAccessor = new Mock<ICorrelationIdAccessor>();
        _mockEventService = new Mock<IPurchaseEventService>();
        _sut = new SupplierReturnService(Context, Mapper, _mockPublishEndpoint.Object, _mockCorrelationIdAccessor.Object, _mockEventService.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedReturn()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "SR-SUPP-001").ConfigureAwait(false);
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = supplier.Id,
            Reason = "Defective goods",
            Notes = "Multiple defects found",
            Lines =
            [
                new CreateSupplierReturnLineRequest { ProductId = 100, WarehouseId = 1, Quantity = 5m }
            ]
        };

        // Act
        Result<SupplierReturnDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(SupplierReturnStatus.Draft));
        result.Value.Reason.Should().Be("Defective goods");
        result.Value.Lines.Should().HaveCount(1);
    }

    [Test]
    public async Task CreateAsync_InactiveSupplier_ReturnsConflict()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "SR-INACT", isActive: false).ConfigureAwait(false);
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = supplier.Id,
            Reason = "Damaged",
            Lines =
            [
                new CreateSupplierReturnLineRequest { ProductId = 100, WarehouseId = 1, Quantity = 5m }
            ]
        };

        // Act
        Result<SupplierReturnDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SUPPLIER_INACTIVE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_GeneratesReturnNumber()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "SR-GEN-SUPP").ConfigureAwait(false);
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = supplier.Id,
            Reason = "Damaged",
            Lines =
            [
                new CreateSupplierReturnLineRequest { ProductId = 100, WarehouseId = 1, Quantity = 5m }
            ]
        };

        // Act
        Result<SupplierReturnDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ReturnNumber.Should().StartWith($"SR-{DateTime.UtcNow:yyyyMMdd}-");
    }

    [Test]
    public async Task ConfirmAsync_DraftReturn_PublishesSupplierReturnCompletedEvent()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "SR-CONF-SUPP").ConfigureAwait(false);
        SupplierReturn sr = await SeedSupplierReturnAsync(supplier.Id).ConfigureAwait(false);

        // Act
        Result<SupplierReturnDetailDto> result = await _sut.ConfirmAsync(sr.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(SupplierReturnStatus.Confirmed));
        result.Value.ConfirmedAtUtc.Should().NotBeNull();
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<SupplierReturnCompletedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ConfirmAsync_AlreadyConfirmed_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "SR-ALRC-SUPP").ConfigureAwait(false);
        SupplierReturn sr = await SeedSupplierReturnAsync(supplier.Id, status: nameof(SupplierReturnStatus.Confirmed)).ConfigureAwait(false);
        sr.ConfirmedAtUtc = DateTime.UtcNow;
        sr.ConfirmedByUserId = 1;
        await Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        Result<SupplierReturnDetailDto> result = await _sut.ConfirmAsync(sr.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("RETURN_ALREADY_CONFIRMED");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CancelAsync_DraftReturn_TransitionsToCancelled()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "SR-CANC-SUPP").ConfigureAwait(false);
        SupplierReturn sr = await SeedSupplierReturnAsync(supplier.Id).ConfigureAwait(false);

        // Act
        Result<SupplierReturnDetailDto> result = await _sut.CancelAsync(sr.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(SupplierReturnStatus.Cancelled));
    }

    [Test]
    public async Task CancelAsync_ConfirmedReturn_ReturnsConflictError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "SR-CNCONF-SUPP").ConfigureAwait(false);
        SupplierReturn sr = await SeedSupplierReturnAsync(supplier.Id, status: nameof(SupplierReturnStatus.Confirmed)).ConfigureAwait(false);

        // Act
        Result<SupplierReturnDetailDto> result = await _sut.CancelAsync(sr.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("RETURN_ALREADY_CONFIRMED");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task GetByIdAsync_ExistingReturn_ReturnsReturnWithLines()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "SR-GET-SUPP").ConfigureAwait(false);
        SupplierReturn sr = await SeedSupplierReturnAsync(supplier.Id).ConfigureAwait(false);

        // Act
        Result<SupplierReturnDetailDto> result = await _sut.GetByIdAsync(sr.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().HaveCount(1);
        result.Value.SupplierName.Should().Be("Test Supplier");
    }

    [Test]
    public async Task SearchAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        Supplier supplier1 = await SeedSupplierAsync(code: "SR-S1").ConfigureAwait(false);
        Supplier supplier2 = await SeedSupplierAsync(code: "SR-S2").ConfigureAwait(false);
        await SeedSupplierReturnAsync(supplier1.Id).ConfigureAwait(false);
        await SeedSupplierReturnAsync(supplier2.Id).ConfigureAwait(false);
        SearchSupplierReturnsRequest request = new() { SupplierId = supplier1.Id };

        // Act
        Result<PaginatedResponse<SupplierReturnDto>> result = await _sut.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
    }

    [Test]
    public async Task CreateAsync_NoLines_ReturnsValidationError()
    {
        // Arrange
        Supplier supplier = await SeedSupplierAsync(code: "SR-NOLINE-SUPP").ConfigureAwait(false);
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = supplier.Id,
            Reason = "Damaged batch",
            Lines = []
        };

        // Act
        Result<SupplierReturnDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("RETURN_MUST_HAVE_LINES");
        result.StatusCode.Should().Be(422);
    }
}
