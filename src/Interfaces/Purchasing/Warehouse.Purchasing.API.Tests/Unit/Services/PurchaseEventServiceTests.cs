using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Correlation;
using Warehouse.Purchasing.API.Services;
using Warehouse.Purchasing.API.Tests.Fixtures;
using Warehouse.Purchasing.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Purchasing;
using Warehouse.ServiceModel.Requests.Purchasing;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Purchasing.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for purchase event recording and search operations.
/// <para>Links to specification SDD-PURCH-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class PurchaseEventServiceTests : PurchasingTestBase
{
    private PurchaseEventService _sut = null!;
    private Mock<IPublishEndpoint> _mockPublishEndpoint = null!;
    private Mock<ICorrelationIdAccessor> _mockCorrelationIdAccessor = null!;
    private Mock<ILogger<PurchaseEventService>> _mockLogger = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockCorrelationIdAccessor = new Mock<ICorrelationIdAccessor>();
        _mockLogger = new Mock<ILogger<PurchaseEventService>>();
        _sut = new PurchaseEventService(Context, Mapper, _mockPublishEndpoint.Object, _mockCorrelationIdAccessor.Object, _mockLogger.Object);
    }

    [Test]
    public async Task RecordEventAsync_ValidEvent_PersistsEvent()
    {
        // Arrange
        string eventType = "PurchaseOrderCreated";
        string entityType = "PurchaseOrder";
        int entityId = 42;
        int userId = 1;
        string payload = "{\"status\":\"Draft\"}";

        // Act
        await _sut.RecordEventAsync(eventType, entityType, entityId, userId, payload, CancellationToken.None).ConfigureAwait(false);

        // Assert
        PurchaseEvent? saved = await Context.PurchaseEvents
            .FirstOrDefaultAsync(e => e.EntityId == entityId)
            .ConfigureAwait(false);
        saved.Should().NotBeNull();
        saved!.EventType.Should().Be("PurchaseOrderCreated");
        saved.EntityType.Should().Be("PurchaseOrder");
        saved.Payload.Should().Be("{\"status\":\"Draft\"}");
    }

    [Test]
    public async Task SearchAsync_ByEventType_ReturnsMatchingEvents()
    {
        // Arrange
        await SeedPurchaseEventAsync(eventType: "PurchaseOrderCreated").ConfigureAwait(false);
        await SeedPurchaseEventAsync(eventType: "PurchaseOrderConfirmed").ConfigureAwait(false);
        await SeedPurchaseEventAsync(eventType: "PurchaseOrderCreated").ConfigureAwait(false);
        SearchPurchaseEventsRequest request = new() { EventType = "PurchaseOrderCreated" };

        // Act
        Result<PaginatedResponse<PurchaseEventDto>> result = await _sut.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
    }

    [Test]
    public async Task SearchAsync_ByEntityTypeAndId_ReturnsMatchingEvents()
    {
        // Arrange
        await SeedPurchaseEventAsync(entityType: "PurchaseOrder", entityId: 10).ConfigureAwait(false);
        await SeedPurchaseEventAsync(entityType: "PurchaseOrder", entityId: 20).ConfigureAwait(false);
        await SeedPurchaseEventAsync(entityType: "GoodsReceipt", entityId: 10).ConfigureAwait(false);
        SearchPurchaseEventsRequest request = new() { EntityType = "PurchaseOrder", EntityId = 10 };

        // Act
        Result<PaginatedResponse<PurchaseEventDto>> result = await _sut.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
    }

    [Test]
    public async Task SearchAsync_ByDateRange_ReturnsMatchingEvents()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        await SeedPurchaseEventAsync(occurredAtUtc: now.AddDays(-5)).ConfigureAwait(false);
        await SeedPurchaseEventAsync(occurredAtUtc: now.AddDays(-1)).ConfigureAwait(false);
        await SeedPurchaseEventAsync(occurredAtUtc: now.AddDays(-10)).ConfigureAwait(false);
        SearchPurchaseEventsRequest request = new()
        {
            DateFrom = now.AddDays(-3),
            DateTo = now
        };

        // Act
        Result<PaginatedResponse<PurchaseEventDto>> result = await _sut.SearchAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
    }
}
