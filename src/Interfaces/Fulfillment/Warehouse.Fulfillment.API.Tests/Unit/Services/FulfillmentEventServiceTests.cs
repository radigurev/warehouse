using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Warehouse.Common.Models;
using Warehouse.Fulfillment.API.Services;
using Warehouse.Fulfillment.API.Tests.Fixtures;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Fulfillment;
using Warehouse.ServiceModel.Requests.Fulfillment;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.Fulfillment.API.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="FulfillmentEventService"/> covering event recording and search.
/// <para>Linked to SDD-FULF-001 section 2.8.</para>
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
public sealed class FulfillmentEventServiceTests : FulfillmentTestBase
{
    private FulfillmentEventService _sut = null!;
    private Mock<IPublishEndpoint> _mockPublishEndpoint = null!;
    private Mock<ILogger<FulfillmentEventService>> _mockLogger = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockLogger = new Mock<ILogger<FulfillmentEventService>>();
        _sut = new FulfillmentEventService(Context, Mapper, _mockPublishEndpoint.Object, _mockLogger.Object);
    }

    [Test]
    public async Task RecordEventAsync_ValidEvent_PersistsEvent()
    {
        // Arrange
        string eventType = "SalesOrderCreated";
        string entityType = "SalesOrder";
        int entityId = 42;
        int userId = 1;

        // Act
        await _sut.RecordEventAsync(eventType, entityType, entityId, userId, null, CancellationToken.None);

        // Assert
        int eventCount = Context.FulfillmentEvents.Count(e => e.EntityId == entityId);
        Assert.That(eventCount, Is.EqualTo(1));
    }

    [Test]
    public async Task RecordEventAsync_WithPayload_PersistsPayload()
    {
        // Arrange
        string payload = "{\"before\":\"Draft\",\"after\":\"Confirmed\"}";

        // Act
        await _sut.RecordEventAsync("SalesOrderConfirmed", "SalesOrder", 1, 1, payload, CancellationToken.None);

        // Assert
        FulfillmentEvent? evt = Context.FulfillmentEvents.FirstOrDefault(e => e.EntityId == 1);
        Assert.That(evt?.Payload, Is.EqualTo(payload));
    }

    [Test]
    public async Task RecordEventAsync_SetsOccurredAtUtc()
    {
        // Arrange
        DateTime beforeCall = DateTime.UtcNow;

        // Act
        await _sut.RecordEventAsync("TestEvent", "TestEntity", 1, 1, null, CancellationToken.None);

        // Assert
        FulfillmentEvent? evt = Context.FulfillmentEvents.FirstOrDefault();
        Assert.That(evt?.OccurredAtUtc, Is.GreaterThanOrEqualTo(beforeCall));
    }

    [Test]
    public async Task SearchAsync_ByEventType_ReturnsMatchingEvents()
    {
        // Arrange
        await SeedFulfillmentEventAsync(eventType: "SalesOrderCreated", entityId: 1);
        await SeedFulfillmentEventAsync(eventType: "SalesOrderConfirmed", entityId: 2);
        await SeedFulfillmentEventAsync(eventType: "SalesOrderCreated", entityId: 3);
        SearchFulfillmentEventsRequest request = new() { EventType = "SalesOrderCreated", Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<FulfillmentEventDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.TotalCount, Is.EqualTo(2));
            Assert.That(result.Value.Items, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task SearchAsync_ByEntityTypeAndId_ReturnsMatchingEvents()
    {
        // Arrange
        await SeedFulfillmentEventAsync(eventType: "Created", entityType: "SalesOrder", entityId: 1);
        await SeedFulfillmentEventAsync(eventType: "Updated", entityType: "SalesOrder", entityId: 1);
        await SeedFulfillmentEventAsync(eventType: "Created", entityType: "PickList", entityId: 1);
        SearchFulfillmentEventsRequest request = new() { EntityType = "SalesOrder", EntityId = 1, Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<FulfillmentEventDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.TotalCount, Is.EqualTo(2));
    }

    [Test]
    public async Task SearchAsync_ByUserId_ReturnsMatchingEvents()
    {
        // Arrange
        await SeedFulfillmentEventAsync(userId: 1);
        await SeedFulfillmentEventAsync(userId: 2);
        await SeedFulfillmentEventAsync(userId: 1);
        SearchFulfillmentEventsRequest request = new() { UserId = 2, Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<FulfillmentEventDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.TotalCount, Is.EqualTo(1));
    }

    [Test]
    public async Task SearchAsync_ByDateRange_ReturnsMatchingEvents()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        await SeedFulfillmentEventAsync(occurredAtUtc: now.AddDays(-5));
        await SeedFulfillmentEventAsync(occurredAtUtc: now.AddDays(-1));
        await SeedFulfillmentEventAsync(occurredAtUtc: now.AddDays(-10));
        SearchFulfillmentEventsRequest request = new() { DateFrom = now.AddDays(-3), DateTo = now, Page = 1, PageSize = 10 };

        // Act
        Result<PaginatedResponse<FulfillmentEventDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.TotalCount, Is.EqualTo(1));
    }

    [Test]
    public async Task SearchAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
            await SeedFulfillmentEventAsync(entityId: i + 1);
        SearchFulfillmentEventsRequest request = new() { Page = 2, PageSize = 2 };

        // Act
        Result<PaginatedResponse<FulfillmentEventDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value!.TotalCount, Is.EqualTo(5));
            Assert.That(result.Value.Items, Has.Count.EqualTo(2));
            Assert.That(result.Value.Page, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task SearchAsync_NoFilters_ReturnsAllEvents()
    {
        // Arrange
        await SeedFulfillmentEventAsync();
        await SeedFulfillmentEventAsync(eventType: "PickListGenerated");
        SearchFulfillmentEventsRequest request = new() { Page = 1, PageSize = 50 };

        // Act
        Result<PaginatedResponse<FulfillmentEventDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Value!.TotalCount, Is.EqualTo(2));
    }
}
