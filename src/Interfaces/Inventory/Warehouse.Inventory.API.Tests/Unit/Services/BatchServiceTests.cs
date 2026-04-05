using FluentAssertions;
using Warehouse.Common.Models;
using Warehouse.Inventory.API.Services;
using Warehouse.Inventory.API.Tests.Fixtures;
using Warehouse.Inventory.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Inventory;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for batch lifecycle operations: CRUD, search, and deactivation.
/// <para>Links to specification SDD-INV-002.</para>
/// </summary>
[TestFixture]
[Category("SDD-INV-002")]
public sealed class BatchServiceTests : InventoryTestBase
{
    private BatchService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _sut = new BatchService(Context, Mapper);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedBatch()
    {
        // Arrange
        Product product = await SeedProductAsync().ConfigureAwait(false);
        CreateBatchRequest request = new()
        {
            ProductId = product.Id,
            BatchNumber = "LOT-2025-001",
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
        };

        // Act
        Result<BatchDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.BatchNumber.Should().Be("LOT-2025-001");
    }

    [Test]
    public async Task CreateAsync_InvalidProduct_ReturnsError()
    {
        // Arrange
        CreateBatchRequest request = new() { ProductId = 9999, BatchNumber = "LOT-X" };

        // Act
        Result<BatchDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PRODUCT");
        result.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task CreateAsync_DuplicateBatchNumber_ReturnsConflict()
    {
        // Arrange
        Product product = await SeedProductAsync().ConfigureAwait(false);
        await SeedBatchAsync(product.Id, "LOT-DUPE").ConfigureAwait(false);
        CreateBatchRequest request = new() { ProductId = product.Id, BatchNumber = "LOT-DUPE" };

        // Act
        Result<BatchDto> result = await _sut.CreateAsync(request, 1, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_BATCH_NUMBER");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task GetByIdAsync_ExistingBatch_ReturnsBatch()
    {
        // Arrange
        Product product = await SeedProductAsync().ConfigureAwait(false);
        Batch batch = await SeedBatchAsync(product.Id).ConfigureAwait(false);

        // Act
        Result<BatchDto> result = await _sut.GetByIdAsync(batch.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(batch.Id);
    }

    [Test]
    public async Task GetByIdAsync_NonExistent_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 9999;

        // Act
        Result<BatchDto> result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("BATCH_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task DeactivateAsync_ActiveBatch_SetsInactive()
    {
        // Arrange
        Product product = await SeedProductAsync().ConfigureAwait(false);
        Batch batch = await SeedBatchAsync(product.Id).ConfigureAwait(false);

        // Act
        Result result = await _sut.DeactivateAsync(batch.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Batch? updated = await Context.Batches.FindAsync(batch.Id).ConfigureAwait(false);
        updated!.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task DeactivateAsync_NonExistent_ReturnsNotFound()
    {
        // Arrange
        int nonExistentId = 9999;

        // Act
        Result result = await _sut.DeactivateAsync(nonExistentId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("BATCH_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }
}
