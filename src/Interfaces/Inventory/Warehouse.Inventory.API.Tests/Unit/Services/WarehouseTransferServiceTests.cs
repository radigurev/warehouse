using FluentAssertions;
using MassTransit;
using Moq;
using Warehouse.Common.Models;
using Warehouse.Common.Workflow;
using Warehouse.Inventory.API.Services.Warehouse;
using Warehouse.Inventory.API.Tests.Fixtures;
using Warehouse.Inventory.API.Workflow.Transfer;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for warehouse transfer state machine: create, complete, cancel.
/// <para>Links to specification SDD-INV-003.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-003")]
public sealed class WarehouseTransferServiceTests : InventoryTestBase
{
    private Mock<IPublishEndpoint> _mockPublishEndpoint = null!;
    private WarehouseTransferService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        IWorkflowEngine<WarehouseTransfer> workflowEngine = CreateTransferWorkflowEngine();
        _sut = new WarehouseTransferService(Context, Mapper, _mockPublishEndpoint.Object, workflowEngine);
    }

    /// <summary>
    /// Creates a workflow engine with all transfer states registered.
    /// </summary>
    private static IWorkflowEngine<WarehouseTransfer> CreateTransferWorkflowEngine()
    {
        List<IWorkflowState<WarehouseTransfer>> states =
        [
            new TransferDraftState(),
            new TransferCompletedState(),
            new TransferCancelledState()
        ];
        return new WorkflowEngine<WarehouseTransfer>(states);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsDraftTransfer()
    {
        // Arrange
        WarehouseEntity source = await SeedWarehouseAsync(code: "SRC-001", name: "Source WH").ConfigureAwait(false);
        WarehouseEntity destination = await SeedWarehouseAsync(code: "DST-001", name: "Destination WH").ConfigureAwait(false);
        Product product = await SeedProductAsync().ConfigureAwait(false);
        CreateTransferRequest request = new()
        {
            SourceWarehouseId = source.Id,
            DestinationWarehouseId = destination.Id,
            Notes = "Routine transfer",
            Lines = [new CreateTransferLineRequest { ProductId = product.Id, Quantity = 50m }]
        };

        // Act
        Result<WarehouseTransferDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Draft");
        result.Value.Lines.Should().HaveCount(1);
    }

    [Test]
    public async Task CreateAsync_SameWarehouse_ReturnsBadRequest()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        Product product = await SeedProductAsync().ConfigureAwait(false);
        CreateTransferRequest request = new()
        {
            SourceWarehouseId = warehouse.Id,
            DestinationWarehouseId = warehouse.Id,
            Lines = [new CreateTransferLineRequest { ProductId = product.Id, Quantity = 10m }]
        };

        // Act
        Result<WarehouseTransferDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("TRANSFER_SAME_WAREHOUSE");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task GetByIdAsync_ExistingTransfer_ReturnsTransfer()
    {
        // Arrange
        WarehouseEntity source = await SeedWarehouseAsync(code: "SRC-002", name: "Source WH 2").ConfigureAwait(false);
        WarehouseEntity destination = await SeedWarehouseAsync(code: "DST-002", name: "Destination WH 2").ConfigureAwait(false);
        WarehouseTransfer transfer = await SeedTransferAsync(source.Id, destination.Id).ConfigureAwait(false);

        // Act
        Result<WarehouseTransferDetailDto> result = await _sut.GetByIdAsync(transfer.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(transfer.Id);
        result.Value.Status.Should().Be("Draft");
    }

    [Test]
    public async Task GetByIdAsync_NonExistent_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 9999;

        // Act
        Result<WarehouseTransferDetailDto> result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("TRANSFER_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task CompleteAsync_DraftTransfer_SetsCompletedStatusAndMovesStock()
    {
        // Arrange
        WarehouseEntity source = await SeedWarehouseAsync(code: "SRC-003", name: "Source WH 3").ConfigureAwait(false);
        WarehouseEntity destination = await SeedWarehouseAsync(code: "DST-003", name: "Destination WH 3").ConfigureAwait(false);
        Product product = await SeedProductAsync(code: "TRANS-P-001").ConfigureAwait(false);
        await SeedStockLevelAsync(product.Id, source.Id, quantityOnHand: 100m).ConfigureAwait(false);
        WarehouseTransfer transfer = await SeedTransferAsync(source.Id, destination.Id, productId: product.Id, quantity: 25m).ConfigureAwait(false);
        CompleteTransferRequest request = new() { Notes = "Transfer completed" };

        // Act
        Result<WarehouseTransferDetailDto> result = await _sut.CompleteAsync(transfer.Id, request, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Completed");
    }

    [Test]
    public async Task CompleteAsync_NonDraftTransfer_ReturnsInvalidStatus()
    {
        // Arrange
        WarehouseEntity source = await SeedWarehouseAsync(code: "SRC-004", name: "Source WH 4").ConfigureAwait(false);
        WarehouseEntity destination = await SeedWarehouseAsync(code: "DST-004", name: "Destination WH 4").ConfigureAwait(false);
        WarehouseTransfer transfer = await SeedTransferAsync(source.Id, destination.Id, status: "Completed").ConfigureAwait(false);
        CompleteTransferRequest request = new();

        // Act
        Result<WarehouseTransferDetailDto> result = await _sut.CompleteAsync(transfer.Id, request, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("TRANSFER_NOT_DRAFT");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CancelAsync_DraftTransfer_SetsCancelledStatus()
    {
        // Arrange
        WarehouseEntity source = await SeedWarehouseAsync(code: "SRC-005", name: "Source WH 5").ConfigureAwait(false);
        WarehouseEntity destination = await SeedWarehouseAsync(code: "DST-005", name: "Destination WH 5").ConfigureAwait(false);
        WarehouseTransfer transfer = await SeedTransferAsync(source.Id, destination.Id).ConfigureAwait(false);

        // Act
        Result result = await _sut.CancelAsync(transfer.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        WarehouseTransfer? cancelled = await Context.WarehouseTransfers.FindAsync(transfer.Id).ConfigureAwait(false);
        cancelled!.Status.Should().Be("Cancelled");
    }

    [Test]
    public async Task CancelAsync_NonDraftTransfer_ReturnsInvalidStatus()
    {
        // Arrange
        WarehouseEntity source = await SeedWarehouseAsync(code: "SRC-006", name: "Source WH 6").ConfigureAwait(false);
        WarehouseEntity destination = await SeedWarehouseAsync(code: "DST-006", name: "Destination WH 6").ConfigureAwait(false);
        WarehouseTransfer transfer = await SeedTransferAsync(source.Id, destination.Id, status: "Completed").ConfigureAwait(false);

        // Act
        Result result = await _sut.CancelAsync(transfer.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("TRANSFER_NOT_DRAFT");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CancelAsync_NonExistent_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 9999;

        // Act
        Result result = await _sut.CancelAsync(nonExistentId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("TRANSFER_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }
}
