using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Services.Stocktake;
using Warehouse.Inventory.API.Tests.Fixtures;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for stocktake session lifecycle: create, start, complete, cancel.
/// <para>Links to specification SDD-INV-004.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-004")]
public sealed class StocktakeSessionServiceTests : InventoryTestBase
{
    private StocktakeSessionService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new StocktakeSessionService(Context, Mapper);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsDraftSession()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        CreateStocktakeSessionRequest request = new()
        {
            WarehouseId = warehouse.Id,
            Name = "Q1 2025 Count",
            Notes = "Full warehouse count"
        };

        // Act
        Result<StocktakeSessionDetailDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Draft");
        result.Value.Name.Should().Be("Q1 2025 Count");
    }

    [Test]
    public async Task GetByIdAsync_ExistingSession_ReturnsSession()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        StocktakeSession session = await SeedStocktakeSessionAsync(warehouse.Id).ConfigureAwait(false);

        // Act
        Result<StocktakeSessionDetailDto> result = await _sut.GetByIdAsync(session.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(session.Id);
    }

    [Test]
    public async Task GetByIdAsync_NonExistent_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 9999;

        // Act
        Result<StocktakeSessionDetailDto> result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SESSION_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task StartAsync_DraftSession_SetsInProgressStatus()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        StocktakeSession session = await SeedStocktakeSessionAsync(warehouse.Id, "Draft").ConfigureAwait(false);

        // Act
        Result<StocktakeSessionDetailDto> result = await _sut.StartAsync(session.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("InProgress");
    }

    [Test]
    public async Task StartAsync_NonDraftSession_ReturnsInvalidStatus()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        StocktakeSession session = await SeedStocktakeSessionAsync(warehouse.Id, "InProgress").ConfigureAwait(false);

        // Act
        Result<StocktakeSessionDetailDto> result = await _sut.StartAsync(session.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SESSION_NOT_DRAFT");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CompleteAsync_InProgressSession_SetsCompletedStatus()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        StocktakeSession session = await SeedStocktakeSessionAsync(warehouse.Id, "InProgress").ConfigureAwait(false);

        // Act
        Result<StocktakeSessionDetailDto> result = await _sut.CompleteAsync(session.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Completed");
    }

    [Test]
    public async Task CompleteAsync_NonInProgressSession_ReturnsInvalidStatus()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        StocktakeSession session = await SeedStocktakeSessionAsync(warehouse.Id, "Draft").ConfigureAwait(false);

        // Act
        Result<StocktakeSessionDetailDto> result = await _sut.CompleteAsync(session.Id, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SESSION_NOT_IN_PROGRESS");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CancelAsync_DraftSession_SetsCancelledStatus()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        StocktakeSession session = await SeedStocktakeSessionAsync(warehouse.Id, "Draft").ConfigureAwait(false);

        // Act
        Result result = await _sut.CancelAsync(session.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        StocktakeSession? cancelled = await Context.StocktakeSessions.FindAsync(session.Id).ConfigureAwait(false);
        cancelled!.Status.Should().Be("Cancelled");
    }

    [Test]
    public async Task CancelAsync_CompletedSession_ReturnsInvalidStatus()
    {
        // Arrange
        WarehouseEntity warehouse = await SeedWarehouseAsync().ConfigureAwait(false);
        StocktakeSession session = await SeedStocktakeSessionAsync(warehouse.Id, "Completed").ConfigureAwait(false);

        // Act
        Result result = await _sut.CancelAsync(session.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SESSION_CANNOT_CANCEL");
        result.StatusCode.Should().Be(409);
    }
}
