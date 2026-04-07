using Microsoft.AspNetCore.Http;
using Moq;
using Warehouse.Infrastructure.Http;
using Warehouse.Infrastructure.Middleware;

namespace Warehouse.Infrastructure.Tests.Http;

/// <summary>
/// Tests for <see cref="CorrelationIdDelegatingHandler"/> covering correlation ID propagation
/// from HttpContext to outbound HTTP request headers.
/// </summary>
[TestFixture]
[Category("SDD-INFRA-001")]
public sealed class CorrelationIdDelegatingHandlerTests
{
    private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;

    [SetUp]
    public void SetUp()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    }

    [Test]
    public async Task SendAsync_CorrelationIdInHttpContext_AddsHeaderToRequest()
    {
        // Arrange
        string expectedCorrelationId = "propagated-correlation-id";
        DefaultHttpContext httpContext = new();
        httpContext.Items[CorrelationIdMiddleware.ItemKey] = expectedCorrelationId;
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        HttpRequestMessage capturedRequest = null!;
        CapturingInnerHandler innerHandler = new(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        CorrelationIdDelegatingHandler handler = new(_httpContextAccessorMock.Object)
        {
            InnerHandler = innerHandler
        };

        HttpMessageInvoker invoker = new(handler);
        HttpRequestMessage requestMessage = new(HttpMethod.Get, "https://example.com/api/test");

        // Act
        await invoker.SendAsync(requestMessage, CancellationToken.None);

        // Assert
        Assert.That(
            capturedRequest.Headers.Contains(CorrelationIdMiddleware.HeaderName),
            Is.True);

        IEnumerable<string> headerValues = capturedRequest.Headers.GetValues(CorrelationIdMiddleware.HeaderName);
        Assert.That(headerValues.First(), Is.EqualTo(expectedCorrelationId));
    }

    [Test]
    public async Task SendAsync_NoCorrelationIdInHttpContext_DoesNotAddHeader()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        HttpRequestMessage capturedRequest = null!;
        CapturingInnerHandler innerHandler = new(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        CorrelationIdDelegatingHandler handler = new(_httpContextAccessorMock.Object)
        {
            InnerHandler = innerHandler
        };

        HttpMessageInvoker invoker = new(handler);
        HttpRequestMessage requestMessage = new(HttpMethod.Get, "https://example.com/api/test");

        // Act
        await invoker.SendAsync(requestMessage, CancellationToken.None);

        // Assert
        Assert.That(
            capturedRequest.Headers.Contains(CorrelationIdMiddleware.HeaderName),
            Is.False);
    }

    [Test]
    public async Task SendAsync_NoHttpContext_DoesNotThrow()
    {
        // Arrange
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        CapturingInnerHandler innerHandler = new(_ =>
            new HttpResponseMessage(System.Net.HttpStatusCode.OK));

        CorrelationIdDelegatingHandler handler = new(_httpContextAccessorMock.Object)
        {
            InnerHandler = innerHandler
        };

        HttpMessageInvoker invoker = new(handler);
        HttpRequestMessage requestMessage = new(HttpMethod.Get, "https://example.com/api/test");

        // Act & Assert
        Assert.That(
            async () => await invoker.SendAsync(requestMessage, CancellationToken.None),
            Throws.Nothing);
    }

    [Test]
    public async Task SendAsync_NoHttpContext_RequestHasNoCorrelationHeader()
    {
        // Arrange
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        HttpRequestMessage capturedRequest = null!;
        CapturingInnerHandler innerHandler = new(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        CorrelationIdDelegatingHandler handler = new(_httpContextAccessorMock.Object)
        {
            InnerHandler = innerHandler
        };

        HttpMessageInvoker invoker = new(handler);
        HttpRequestMessage requestMessage = new(HttpMethod.Get, "https://example.com/api/test");

        // Act
        await invoker.SendAsync(requestMessage, CancellationToken.None);

        // Assert
        Assert.That(
            capturedRequest.Headers.Contains(CorrelationIdMiddleware.HeaderName),
            Is.False);
    }

    /// <summary>
    /// Inner handler that captures the outbound request and returns a configurable response.
    /// Used to inspect headers added by the delegating handler under test.
    /// </summary>
    private sealed class CapturingInnerHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        /// <summary>
        /// Initializes a new instance with the specified response factory.
        /// </summary>
        public CapturingInnerHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        /// <summary>
        /// Captures the request and returns the configured response.
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
