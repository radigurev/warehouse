using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Warehouse.Infrastructure.Authorization;
using Warehouse.ServiceModel.Responses.Auth;

namespace Warehouse.Infrastructure.Tests.Authorization;

/// <summary>
/// Tests for <see cref="UserPermissionService"/> covering Redis cache hits, cache misses
/// with Auth.API HTTP fallback, cache population, and fail-closed behavior.
/// <para>Covers CHG-ENH-004 test plan: PermissionService_CacheHit, CacheMiss, PopulatesRedis, BothUnavailable.</para>
/// </summary>
[TestFixture]
[Category("CHG-ENH-004")]
public sealed class UserPermissionServiceTests
{
    private Mock<IDistributedCache> _cacheMock = null!;
    private Mock<ILogger<UserPermissionService>> _loggerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<UserPermissionService>>();
    }

    [Test]
    public async Task GetPermissionsAsync_CacheHit_ReturnsFromRedisWithoutHttpCall()
    {
        // Arrange
        List<string> expectedPermissions = ["inventory:read", "customers:write"];
        byte[] cached = JsonSerializer.SerializeToUtf8Bytes(expectedPermissions);

        _cacheMock
            .Setup(c => c.GetAsync(UserPermissionService.BuildCacheKey(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        FakeHttpMessageHandler handler = new(_ =>
            throw new InvalidOperationException("HTTP call should not be made on cache hit"));
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost:5001/") };

        UserPermissionService service = new(_cacheMock.Object, httpClient, _loggerMock.Object);

        // Act
        IReadOnlySet<string> result = await service.GetPermissionsAsync(1, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EquivalentTo(expectedPermissions));
    }

    [Test]
    public async Task GetPermissionsAsync_CacheMiss_CallsAuthApi()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        bool httpCalled = false;
        FakeHttpMessageHandler handler = new(request =>
        {
            httpCalled = true;
            UserPermissionsResponse body = new()
            {
                UserId = 1,
                Permissions = ["inventory:read", "inventory:write"]
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(body)
            };
        });
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost:5001/") };

        UserPermissionService service = new(_cacheMock.Object, httpClient, _loggerMock.Object);

        // Act
        IReadOnlySet<string> result = await service.GetPermissionsAsync(1, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(httpCalled, Is.True, "Auth.API should be called on cache miss");
            Assert.That(result, Does.Contain("inventory:read"));
            Assert.That(result, Does.Contain("inventory:write"));
        });
    }

    [Test]
    public async Task GetPermissionsAsync_CacheMiss_PopulatesRedis()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        FakeHttpMessageHandler handler = new(_ =>
        {
            UserPermissionsResponse body = new()
            {
                UserId = 1,
                Permissions = ["customers:read"]
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(body)
            };
        });
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost:5001/") };

        UserPermissionService service = new(_cacheMock.Object, httpClient, _loggerMock.Object);

        // Act
        await service.GetPermissionsAsync(1, CancellationToken.None);

        // Assert
        _cacheMock.Verify(
            c => c.SetAsync(
                UserPermissionService.BuildCacheKey(1),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Permissions should be written to Redis after HTTP fallback");
    }

    [Test]
    public async Task GetPermissionsAsync_BothUnavailable_ReturnsEmptySet()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Redis unavailable"));

        FakeHttpMessageHandler handler = new(_ =>
            throw new HttpRequestException("Auth.API unavailable"));
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost:5001/") };

        UserPermissionService service = new(_cacheMock.Object, httpClient, _loggerMock.Object);

        // Act
        IReadOnlySet<string> result = await service.GetPermissionsAsync(1, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Empty, "Fail-closed: empty set when both Redis and Auth.API are unavailable");
    }

    [Test]
    public async Task GetPermissionsAsync_CacheHit_DoesNotCallSetAsync()
    {
        // Arrange
        List<string> permissions = ["inventory:read"];
        byte[] cached = JsonSerializer.SerializeToUtf8Bytes(permissions);

        _cacheMock
            .Setup(c => c.GetAsync(UserPermissionService.BuildCacheKey(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        FakeHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK));
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost:5001/") };

        UserPermissionService service = new(_cacheMock.Object, httpClient, _loggerMock.Object);

        // Act
        await service.GetPermissionsAsync(1, CancellationToken.None);

        // Assert
        _cacheMock.Verify(
            c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "Cache should not be written on a cache hit");
    }

    [Test]
    public async Task GetPermissionsAsync_AuthApiReturnsNull_ReturnsEmptySet()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        FakeHttpMessageHandler handler = new(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
            });
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost:5001/") };

        UserPermissionService service = new(_cacheMock.Object, httpClient, _loggerMock.Object);

        // Act
        IReadOnlySet<string> result = await service.GetPermissionsAsync(1, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task BuildCacheKey_ReturnsExpectedFormat()
    {
        // Arrange & Act
        string key = UserPermissionService.BuildCacheKey(42);

        // Assert
        Assert.That(key, Is.EqualTo("auth:user:42:permissions"));
    }

    /// <summary>
    /// Fake HTTP message handler that returns a configurable response for testing.
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        /// <summary>
        /// Initializes a new instance with the specified response factory.
        /// </summary>
        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        /// <summary>
        /// Returns the configured response for any request.
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
