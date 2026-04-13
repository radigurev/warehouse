using FluentAssertions;
using MassTransit;
using Moq;
using Warehouse.Common.Models;
using Warehouse.Common.Workflow;
using Warehouse.Inventory.API.Services.Stock;
using Warehouse.Inventory.API.Tests.Fixtures;
using Warehouse.Inventory.API.Workflow.Adjustment;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for inventory adjustment state machine: create, approve, reject, and apply.
/// <para>Links to specification SDD-INV-002.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-002")]
public sealed class InventoryAdjustmentServiceTests : InventoryTestBase
{
    private Mock<IPublishEndpoint> _mockPublishEndpoint = null!;
    private InventoryAdjustmentService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        IWorkflowEngine<InventoryAdjustment> workflowEngine = CreateAdjustmentWorkflowEngine();
        _sut = new InventoryAdjustmentService(Context, Mapper, _mockPublishEndpoint.Object, workflowEngine);
    }

    /// <summary>
    /// Creates a workflow engine with all adjustment states registered.
    /// </summary>
    private static IWorkflowEngine<InventoryAdjustment> CreateAdjustmentWorkflowEngine()
    {
        List<IWorkflowState<InventoryAdjustment>> states =
        [
            new AdjustmentPendingState(),
            new AdjustmentApprovedState(),
            new AdjustmentRejectedState(),
            new AdjustmentAppliedState()
        ];
        return new WorkflowEngine<InventoryAdjustment>(states);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsAdjustmentWithPendingStatus()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        Product product = await SeedProductAsync().ConfigureAwait(false);
        CreateAdjustmentRequest request = new()
        {
            WarehouseId = warehouse.Id,
            Reason = "Cycle count discrepancy",
            Lines =
            [
                new CreateAdjustmentLineRequest { ProductId = product.Id, ActualQuantity = 95m }
            ]
        };

        // Act
        Result<InventoryAdjustmentDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Pending");
        result.Value.Reason.Should().Be("Cycle count discrepancy");
        result.Value.Lines.Should().HaveCount(1);
    }

    [Test]
    public async Task GetByIdAsync_ExistingAdjustment_ReturnsAdjustment()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        Product product = await SeedProductAsync().ConfigureAwait(false);
        InventoryAdjustment adjustment = await SeedAdjustmentAsync("Pending", warehouse.Id, product.Id).ConfigureAwait(false);

        // Act
        Result<InventoryAdjustmentDetailDto> result = await _sut.GetByIdAsync(adjustment.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(adjustment.Id);
    }

    [Test]
    public async Task GetByIdAsync_NonExistent_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 9999;

        // Act
        Result<InventoryAdjustmentDetailDto> result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ADJUSTMENT_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task ApproveAsync_PendingAdjustment_SetsApprovedStatus()
    {
        // Arrange
        InventoryAdjustment adjustment = await SeedAdjustmentAsync("Pending").ConfigureAwait(false);
        ApproveAdjustmentRequest request = new() { Notes = "Looks correct" };

        // Act
        Result<InventoryAdjustmentDetailDto> result = await _sut.ApproveAsync(adjustment.Id, request, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Approved");
    }

    [Test]
    public async Task ApproveAsync_NonPendingAdjustment_ReturnsInvalidStatus()
    {
        // Arrange
        InventoryAdjustment adjustment = await SeedAdjustmentAsync("Approved").ConfigureAwait(false);
        ApproveAdjustmentRequest request = new();

        // Act
        Result<InventoryAdjustmentDetailDto> result = await _sut.ApproveAsync(adjustment.Id, request, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ADJUSTMENT_NOT_PENDING");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task RejectAsync_PendingAdjustment_SetsRejectedStatus()
    {
        // Arrange
        InventoryAdjustment adjustment = await SeedAdjustmentAsync("Pending").ConfigureAwait(false);
        RejectAdjustmentRequest request = new() { Notes = "Incorrect counts" };

        // Act
        Result<InventoryAdjustmentDetailDto> result = await _sut.RejectAsync(adjustment.Id, request, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Rejected");
    }

    [Test]
    public async Task RejectAsync_NonPendingAdjustment_ReturnsInvalidStatus()
    {
        // Arrange
        InventoryAdjustment adjustment = await SeedAdjustmentAsync("Rejected").ConfigureAwait(false);
        RejectAdjustmentRequest request = new() { Notes = "Already rejected" };

        // Act
        Result<InventoryAdjustmentDetailDto> result = await _sut.RejectAsync(adjustment.Id, request, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ADJUSTMENT_NOT_PENDING");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task ApplyAsync_ApprovedAdjustment_SetsAppliedStatusAndUpdatesStock()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        Product product = await SeedProductAsync().ConfigureAwait(false);
        await SeedStockLevelAsync(product.Id, warehouse.Id, quantityOnHand: 100m).ConfigureAwait(false);
        InventoryAdjustment adjustment = await SeedAdjustmentAsync("Approved", warehouse.Id, product.Id).ConfigureAwait(false);

        // Act
        Result<InventoryAdjustmentDetailDto> result = await _sut.ApplyAsync(adjustment.Id, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Applied");
    }

    [Test]
    public async Task ApplyAsync_NonApprovedAdjustment_ReturnsInvalidStatus()
    {
        // Arrange
        InventoryAdjustment adjustment = await SeedAdjustmentAsync("Pending").ConfigureAwait(false);

        // Act
        Result<InventoryAdjustmentDetailDto> result = await _sut.ApplyAsync(adjustment.Id, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ADJUSTMENT_NOT_APPROVED");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task ApplyAsync_NotFound_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 9999;

        // Act
        Result<InventoryAdjustmentDetailDto> result = await _sut.ApplyAsync(nonExistentId, 2, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ADJUSTMENT_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }
}
