using FluentAssertions;
using MassTransit;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Warehouse.EventLog.API.Consumers;
using Warehouse.EventLog.DBModel;
using Warehouse.EventLog.DBModel.Models;
using Warehouse.ServiceModel.Events;

namespace Warehouse.EventLog.API.Tests.Unit.Consumers;

[TestFixture]
public sealed class AuthAuditLoggedEventConsumerTests
{
    private EventLogDbContext _context = null!;
    private AuthAuditLoggedEventConsumer _sut = null!;
    private Mock<ILogger<AuthAuditLoggedEventConsumer>> _mockLogger = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<EventLogDbContext> options = new DbContextOptionsBuilder<EventLogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EventLogDbContext(options);
        _mockLogger = new Mock<ILogger<AuthAuditLoggedEventConsumer>>();
        _sut = new AuthAuditLoggedEventConsumer(_context, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task Consume_ValidEvent_PersistsAuthEvent()
    {
        AuthAuditLoggedEvent message = new()
        {
            UserId = 1,
            Action = "CreateUser",
            Resource = "users",
            Details = "{\"username\":\"testuser\"}",
            IpAddress = "127.0.0.1",
            Username = "admin",
            OccurredAtUtc = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString()
        };

        Mock<ConsumeContext<AuthAuditLoggedEvent>> mockContext = new();
        mockContext.Setup(c => c.Message).Returns(message);

        await _sut.Consume(mockContext.Object);

        AuthEvent? persisted = await _context.AuthEvents.FirstOrDefaultAsync();
        persisted.Should().NotBeNull();
        persisted!.Domain.Should().Be("Auth");
        persisted.Action.Should().Be("CreateUser");
        persisted.Resource.Should().Be("users");
        persisted.Username.Should().Be("admin");
        persisted.IpAddress.Should().Be("127.0.0.1");
        persisted.CorrelationId.Should().Be(message.CorrelationId);
    }

    [Test]
    public async Task Consume_SetsReceivedAtUtc()
    {
        DateTime before = DateTime.UtcNow;

        AuthAuditLoggedEvent message = new()
        {
            UserId = 1,
            Action = "Login",
            Resource = "auth",
            OccurredAtUtc = DateTime.UtcNow.AddSeconds(-5)
        };

        Mock<ConsumeContext<AuthAuditLoggedEvent>> mockContext = new();
        mockContext.Setup(c => c.Message).Returns(message);

        await _sut.Consume(mockContext.Object);

        AuthEvent? persisted = await _context.AuthEvents.FirstOrDefaultAsync();
        persisted.Should().NotBeNull();
        persisted!.ReceivedAtUtc.Should().BeOnOrAfter(before);
    }

    [Test]
    public async Task Consume_SetsDomainToAuth()
    {
        AuthAuditLoggedEvent message = new()
        {
            UserId = 1,
            Action = "AssignRole",
            Resource = "roles",
            OccurredAtUtc = DateTime.UtcNow
        };

        Mock<ConsumeContext<AuthAuditLoggedEvent>> mockContext = new();
        mockContext.Setup(c => c.Message).Returns(message);

        await _sut.Consume(mockContext.Object);

        AuthEvent? persisted = await _context.AuthEvents.FirstOrDefaultAsync();
        persisted.Should().NotBeNull();
        persisted!.Domain.Should().Be("Auth");
    }

    [Test]
    public async Task Consume_DuplicateEvent_SkipsSilently()
    {
        AuthAuditLoggedEvent message = new()
        {
            UserId = 1,
            Action = "CreateUser",
            Resource = "users",
            OccurredAtUtc = new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc)
        };

        Mock<ConsumeContext<AuthAuditLoggedEvent>> mockContext = new();
        mockContext.Setup(c => c.Message).Returns(message);

        await _sut.Consume(mockContext.Object);
        await _sut.Consume(mockContext.Object);

        int count = await _context.AuthEvents.CountAsync();
        count.Should().Be(1);
    }
}
