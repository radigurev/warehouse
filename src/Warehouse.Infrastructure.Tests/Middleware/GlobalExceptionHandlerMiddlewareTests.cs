using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Warehouse.Infrastructure.Middleware;

namespace Warehouse.Infrastructure.Tests.Middleware;

/// <summary>
/// Tests for <see cref="GlobalExceptionHandlerMiddleware"/> covering pass-through behavior,
/// exception handling, ProblemDetails response formatting, and content type.
/// </summary>
[TestFixture]
[Category("SDD-INFRA-001")]
public sealed class GlobalExceptionHandlerMiddlewareTests
{
    private Mock<ILogger<GlobalExceptionHandlerMiddleware>> _loggerMock = null!;
    private DefaultHttpContext _httpContext = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [TearDown]
    public void TearDown()
    {
        _httpContext.Response.Body.Dispose();
    }

    [Test]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        // Arrange
        bool nextCalled = false;
        GlobalExceptionHandlerMiddleware middleware = new(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.That(nextCalled, Is.True);
    }

    [Test]
    public async Task InvokeAsync_NoException_DoesNotModifyStatusCode()
    {
        // Arrange
        GlobalExceptionHandlerMiddleware middleware = new(
            _ => Task.CompletedTask,
            _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task InvokeAsync_UnhandledException_Returns500StatusCode()
    {
        // Arrange
        GlobalExceptionHandlerMiddleware middleware = new(
            _ => throw new InvalidOperationException("Test exception"),
            _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.That(_httpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
    }

    [Test]
    public async Task InvokeAsync_UnhandledException_ReturnsProblemDetailsJson()
    {
        // Arrange
        _httpContext.Request.Path = "/api/v1/test";
        GlobalExceptionHandlerMiddleware middleware = new(
            _ => throw new InvalidOperationException("Test exception"),
            _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        StreamReader reader = new(_httpContext.Response.Body);
        string body = await reader.ReadToEndAsync();

        JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        ProblemDetails? problemDetails = JsonSerializer.Deserialize<ProblemDetails>(body, options);

        Assert.Multiple(() =>
        {
            Assert.That(problemDetails, Is.Not.Null);
            Assert.That(problemDetails!.Status, Is.EqualTo(500));
            Assert.That(problemDetails.Title, Is.EqualTo("Internal Server Error"));
            Assert.That(problemDetails.Detail, Is.EqualTo("An unexpected error occurred. Please try again later."));
            Assert.That(problemDetails.Instance, Is.EqualTo("/api/v1/test"));
            Assert.That(problemDetails.Type, Is.EqualTo("https://warehouse.local/errors/INTERNAL_ERROR"));
        });
    }

    [Test]
    public async Task InvokeAsync_UnhandledException_SetsJsonContentType()
    {
        // Arrange
        GlobalExceptionHandlerMiddleware middleware = new(
            _ => throw new InvalidOperationException("Test exception"),
            _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.That(_httpContext.Response.ContentType, Is.EqualTo("application/problem+json"));
    }

    [Test]
    public async Task InvokeAsync_UnhandledException_LogsError()
    {
        // Arrange
        GlobalExceptionHandlerMiddleware middleware = new(
            _ => throw new InvalidOperationException("Logged exception"),
            _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
