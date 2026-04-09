using AutoMapper;
using FluentAssertions;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Warehouse.Common.Models;
using Warehouse.EventLog.API.Services;
using Warehouse.EventLog.DBModel;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.Mapping.Profiles.EventLog;
using Warehouse.ServiceModel.DTOs.EventLog;
using Warehouse.ServiceModel.Requests.EventLog;
using Warehouse.ServiceModel.Responses;

namespace Warehouse.EventLog.API.Tests.Unit.Services;

[TestFixture]
public sealed class EventQueryServiceTests
{
    private EventLogDbContext _context = null!;
    private EventQueryService _sut = null!;
    private IMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<EventLogDbContext> options = new DbContextOptionsBuilder<EventLogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EventLogDbContext(options);
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<EventLogMappingProfile>()).CreateMapper();
        _sut = new EventQueryService(_context, _mapper);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task SearchAsync_NoFilters_ReturnsAllEventsPaginated()
    {
        _context.AuthEvents.Add(CreateAuthEvent("CreateUser", "users", 1));
        _context.PurchaseEvents.Add(CreatePurchaseEvent("PurchaseOrderCreated", "PurchaseOrder", 1));
        await _context.SaveChangesAsync();

        SearchEventsRequest request = new() { Page = 1, PageSize = 25 };
        Result<PaginatedResponse<OperationsEventDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task SearchAsync_FilterByDomain_ReturnsOnlyDomainEvents()
    {
        _context.AuthEvents.Add(CreateAuthEvent("CreateUser", "users", 1));
        _context.PurchaseEvents.Add(CreatePurchaseEvent("PurchaseOrderCreated", "PurchaseOrder", 1));
        await _context.SaveChangesAsync();

        SearchEventsRequest request = new() { Domain = "Auth", Page = 1, PageSize = 25 };
        Result<PaginatedResponse<OperationsEventDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Should().AllSatisfy(e => e.Domain.Should().Be("Auth"));
    }

    [Test]
    public async Task SearchAsync_DefaultSort_OccurredAtUtcDescending()
    {
        DateTime older = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime newer = new(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc);

        _context.AuthEvents.Add(CreateAuthEvent("Login", "auth", 1, older));
        _context.AuthEvents.Add(CreateAuthEvent("Logout", "auth", 2, newer));
        await _context.SaveChangesAsync();

        SearchEventsRequest request = new() { Page = 1, PageSize = 25 };
        Result<PaginatedResponse<OperationsEventDto>> result = await _sut.SearchAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items[0].OccurredAtUtc.Should().BeAfter(result.Value.Items[1].OccurredAtUtc);
    }

    [Test]
    public async Task GetByIdAsync_ExistingEvent_ReturnsEventWithPayload()
    {
        AuthEvent authEvent = CreateAuthEvent("CreateUser", "users", 1);
        authEvent.Payload = "{\"test\":\"data\"}";
        _context.AuthEvents.Add(authEvent);
        await _context.SaveChangesAsync();

        Result<OperationsEventDto> result = await _sut.GetByIdAsync(authEvent.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Payload.Should().Be("{\"test\":\"data\"}");
    }

    [Test]
    public async Task GetByIdAsync_NonExistent_Returns404()
    {
        Result<OperationsEventDto> result = await _sut.GetByIdAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("EVENT_NOT_FOUND");
    }

    [Test]
    public async Task GetCorrelationTimelineAsync_ReturnsChronologicalEvents()
    {
        string correlationId = Guid.NewGuid().ToString();
        DateTime t1 = new(2026, 4, 9, 10, 0, 0, DateTimeKind.Utc);
        DateTime t2 = new(2026, 4, 9, 10, 0, 1, DateTimeKind.Utc);

        AuthEvent e1 = CreateAuthEvent("Login", "auth", 1, t1);
        e1.CorrelationId = correlationId;
        PurchaseEvent e2 = CreatePurchaseEvent("PurchaseOrderCreated", "PurchaseOrder", 1, t2);
        e2.CorrelationId = correlationId;

        _context.AuthEvents.Add(e1);
        _context.PurchaseEvents.Add(e2);
        await _context.SaveChangesAsync();

        Result<IReadOnlyList<OperationsEventDto>> result = await _sut
            .GetCorrelationTimelineAsync(correlationId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
        result.Value[0].OccurredAtUtc.Should().BeBefore(result.Value[1].OccurredAtUtc);
    }

    private static AuthEvent CreateAuthEvent(string action, string resource, int entityId, DateTime? occurredAt = null)
    {
        return new AuthEvent
        {
            Domain = "Auth",
            EventType = action,
            EntityType = resource,
            EntityId = entityId,
            UserId = 1,
            OccurredAtUtc = occurredAt ?? DateTime.UtcNow,
            ReceivedAtUtc = DateTime.UtcNow,
            Action = action,
            Resource = resource
        };
    }

    private static PurchaseEvent CreatePurchaseEvent(string eventType, string entityType, int entityId, DateTime? occurredAt = null)
    {
        return new PurchaseEvent
        {
            Domain = "Purchasing",
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            UserId = 1,
            OccurredAtUtc = occurredAt ?? DateTime.UtcNow,
            ReceivedAtUtc = DateTime.UtcNow
        };
    }
}
