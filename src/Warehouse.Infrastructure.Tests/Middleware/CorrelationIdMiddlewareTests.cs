using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Warehouse.Infrastructure.Middleware;

namespace Warehouse.Infrastructure.Tests.Middleware;

/// <summary>
/// Tests for <see cref="CorrelationIdMiddleware"/> covering header generation,
/// propagation of existing headers, response header setting, and context item storage.
/// </summary>
[TestFixture]
[Category("SDD-INFRA-001")]
public sealed class CorrelationIdMiddlewareTests
{
    private DefaultHttpContext _httpContext = null!;
    private TrackingResponseFeature _responseFeature = null!;
    private bool _nextDelegateCalled;

    [SetUp]
    public void SetUp()
    {
        _httpContext = new DefaultHttpContext();
        _responseFeature = new TrackingResponseFeature(_httpContext.Features.Get<IHttpResponseFeature>()!);
        _httpContext.Features.Set<IHttpResponseFeature>(_responseFeature);
        _nextDelegateCalled = false;
    }

    [Test]
    public async Task InvokeAsync_NoCorrelationIdHeader_GeneratesNewGuid()
    {
        // Arrange
        CorrelationIdMiddleware middleware = new(_ =>
        {
            _nextDelegateCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        object? storedValue = _httpContext.Items[CorrelationIdMiddleware.ItemKey];
        Assert.That(storedValue, Is.Not.Null);
        Assert.That(Guid.TryParse(storedValue!.ToString(), out _), Is.True);
    }

    [Test]
    public async Task InvokeAsync_ExistingCorrelationIdHeader_UsesProvidedValue()
    {
        // Arrange
        string expectedCorrelationId = "my-custom-correlation-id";
        _httpContext.Request.Headers[CorrelationIdMiddleware.HeaderName] = expectedCorrelationId;

        CorrelationIdMiddleware middleware = new(_ =>
        {
            _nextDelegateCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        object? storedValue = _httpContext.Items[CorrelationIdMiddleware.ItemKey];
        Assert.That(storedValue, Is.EqualTo(expectedCorrelationId));
    }

    [Test]
    public async Task InvokeAsync_SetsResponseHeader()
    {
        // Arrange
        string expectedCorrelationId = "test-correlation-123";
        _httpContext.Request.Headers[CorrelationIdMiddleware.HeaderName] = expectedCorrelationId;

        CorrelationIdMiddleware middleware = new(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(_httpContext);
        await _responseFeature.FireOnStartingAsync();

        // Assert
        string? responseHeader = _httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault();
        Assert.That(responseHeader, Is.EqualTo(expectedCorrelationId));
    }

    [Test]
    public async Task InvokeAsync_StoresInHttpContextItems()
    {
        // Arrange
        string expectedCorrelationId = "stored-in-items-id";
        _httpContext.Request.Headers[CorrelationIdMiddleware.HeaderName] = expectedCorrelationId;

        CorrelationIdMiddleware middleware = new(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.That(_httpContext.Items.ContainsKey(CorrelationIdMiddleware.ItemKey), Is.True);
        Assert.That(_httpContext.Items[CorrelationIdMiddleware.ItemKey], Is.EqualTo(expectedCorrelationId));
    }

    [Test]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        // Arrange
        CorrelationIdMiddleware middleware = new(_ =>
        {
            _nextDelegateCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.That(_nextDelegateCalled, Is.True);
    }

    [Test]
    public async Task InvokeAsync_NoHeader_GeneratedIdIsStoredAndReturnedInResponse()
    {
        // Arrange
        CorrelationIdMiddleware middleware = new(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(_httpContext);
        await _responseFeature.FireOnStartingAsync();

        // Assert
        string? storedId = _httpContext.Items[CorrelationIdMiddleware.ItemKey] as string;
        string? responseHeader = _httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault();

        Assert.Multiple(() =>
        {
            Assert.That(storedId, Is.Not.Null.And.Not.Empty);
            Assert.That(responseHeader, Is.EqualTo(storedId));
        });
    }

    /// <summary>
    /// Decorating HTTP response feature that intercepts OnStarting callbacks while delegating
    /// all other operations to the inner feature. Enables testing of response header setup.
    /// </summary>
    private sealed class TrackingResponseFeature : IHttpResponseFeature
    {
        private readonly IHttpResponseFeature _inner;
        private readonly List<(Func<object, Task> Callback, object State)> _onStartingCallbacks = [];

        /// <summary>
        /// Initializes a new instance decorating the specified inner feature.
        /// </summary>
        public TrackingResponseFeature(IHttpResponseFeature inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Gets or sets the status code, delegated to the inner feature.
        /// </summary>
        public int StatusCode
        {
            get => _inner.StatusCode;
            set => _inner.StatusCode = value;
        }

        /// <summary>
        /// Gets or sets the reason phrase, delegated to the inner feature.
        /// </summary>
        public string? ReasonPhrase
        {
            get => _inner.ReasonPhrase;
            set => _inner.ReasonPhrase = value;
        }

        /// <summary>
        /// Gets or sets the headers, delegated to the inner feature.
        /// </summary>
        public IHeaderDictionary Headers
        {
            get => _inner.Headers;
            set => _inner.Headers = value;
        }

        /// <summary>
        /// Gets or sets the body stream, delegated to the inner feature.
        /// </summary>
#pragma warning disable CS0618
        public Stream Body
        {
            get => _inner.Body;
            set => _inner.Body = value;
        }
#pragma warning restore CS0618

        /// <summary>
        /// Gets whether the response has started, delegated to the inner feature.
        /// </summary>
        public bool HasStarted => _inner.HasStarted;

        /// <summary>
        /// Intercepts the OnStarting callback and stores it for later invocation.
        /// </summary>
        public void OnStarting(Func<object, Task> callback, object state)
        {
            _onStartingCallbacks.Add((callback, state));
            _inner.OnStarting(callback, state);
        }

        /// <summary>
        /// Delegates to the inner feature OnCompleted.
        /// </summary>
        public void OnCompleted(Func<object, Task> callback, object state)
        {
            _inner.OnCompleted(callback, state);
        }

        /// <summary>
        /// Fires all registered OnStarting callbacks to simulate response start.
        /// </summary>
        public async Task FireOnStartingAsync()
        {
            foreach ((Func<object, Task> callback, object state) in _onStartingCallbacks)
            {
                await callback(state);
            }
        }
    }
}
