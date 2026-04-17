using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using NUnit.Framework;
using Warehouse.Common.Models;
using Warehouse.Nomenclature.API.Services;
using Warehouse.Nomenclature.API.Tests.Fixtures;
using Warehouse.Nomenclature.DBModel.Models;
using Warehouse.ServiceModel.DTOs.Nomenclature;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Tests.Unit.Services;

/// <summary>
/// Unit tests for currency CRUD, deactivation, and reactivation per SDD-NOM-001 section 7.4.
/// <para>Links to specification SDD-NOM-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-NOM-001")]
public sealed class CurrencyServiceTests : NomenclatureTestBase
{
    private Mock<IDistributedCache> _mockCache = null!;
    private CurrencyService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockCache = new Mock<IDistributedCache>();
        _sut = new CurrencyService(Context, Mapper, _mockCache.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedCurrency()
    {
        // Arrange
        CreateCurrencyRequest request = new()
        {
            Code = "USD",
            Name = "US Dollar",
            Symbol = "$"
        };

        // Act
        Result<CurrencyDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("USD");
        result.Value.Name.Should().Be("US Dollar");
        result.Value.Symbol.Should().Be("$");
        result.Value.IsActive.Should().BeTrue();
        result.Value.Id.Should().BeGreaterThan(0);
        result.Value.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task CreateAsync_DuplicateCode_ReturnsConflictError()
    {
        // Arrange
        await SeedCurrencyAsync(code: "BGN", name: "Bulgarian Lev").ConfigureAwait(false);
        CreateCurrencyRequest request = new()
        {
            Code = "BGN",
            Name = "Duplicate Code"
        };

        // Act
        Result<CurrencyDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CURRENCY_CODE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task UpdateAsync_ExistingCurrency_ReturnsUpdatedCurrency()
    {
        // Arrange
        Currency seeded = await SeedCurrencyAsync(code: "EUR", name: "Euro", symbol: "E").ConfigureAwait(false);
        UpdateCurrencyRequest request = new()
        {
            Name = "European Euro",
            Symbol = "EUR"
        };

        // Act
        Result<CurrencyDto> result = await _sut.UpdateAsync(seeded.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("European Euro");
        result.Value.Symbol.Should().Be("EUR");
        result.Value.ModifiedAtUtc.Should().NotBeNull();
        result.Value.ModifiedAtUtc!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task UpdateAsync_NonExistentCurrency_ReturnsNotFoundError()
    {
        // Arrange
        UpdateCurrencyRequest request = new() { Name = "Nowhere Dollar" };

        // Act
        Result<CurrencyDto> result = await _sut.UpdateAsync(999, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CURRENCY_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task DeactivateAsync_ActiveCurrency_SetsInactive()
    {
        // Arrange
        Currency seeded = await SeedCurrencyAsync(isActive: true).ConfigureAwait(false);

        // Act
        Result<CurrencyDto> result = await _sut.DeactivateAsync(seeded.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task DeactivateAsync_AlreadyInactive_ReturnsConflictError()
    {
        // Arrange
        Currency seeded = await SeedCurrencyAsync(isActive: false).ConfigureAwait(false);

        // Act
        Result<CurrencyDto> result = await _sut.DeactivateAsync(seeded.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CURRENCY_ALREADY_INACTIVE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task ReactivateAsync_InactiveCurrency_SetsActive()
    {
        // Arrange
        Currency seeded = await SeedCurrencyAsync(isActive: false).ConfigureAwait(false);

        // Act
        Result<CurrencyDto> result = await _sut.ReactivateAsync(seeded.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task ReactivateAsync_AlreadyActive_ReturnsConflictError()
    {
        // Arrange
        Currency seeded = await SeedCurrencyAsync(isActive: true).ConfigureAwait(false);

        // Act
        Result<CurrencyDto> result = await _sut.ReactivateAsync(seeded.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CURRENCY_ALREADY_ACTIVE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task ListAsync_ReturnsActiveCurrenciesSortedByName()
    {
        // Arrange
        await SeedCurrencyAsync(code: "USD", name: "US Dollar", isActive: true).ConfigureAwait(false);
        await SeedCurrencyAsync(code: "BGN", name: "Bulgarian Lev", isActive: true).ConfigureAwait(false);
        await SeedCurrencyAsync(code: "JPY", name: "Japanese Yen", isActive: false).ConfigureAwait(false);

        // Act
        Result<IReadOnlyList<CurrencyDto>> result = await _sut.ListAsync(false, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Name.Should().Be("Bulgarian Lev");
        result.Value[1].Name.Should().Be("US Dollar");
    }

    [Test]
    public async Task ListAsync_IncludeInactive_ReturnsAllCurrencies()
    {
        // Arrange
        await SeedCurrencyAsync(code: "USD", name: "US Dollar", isActive: true).ConfigureAwait(false);
        await SeedCurrencyAsync(code: "BGN", name: "Bulgarian Lev", isActive: true).ConfigureAwait(false);
        await SeedCurrencyAsync(code: "JPY", name: "Japanese Yen", isActive: false).ConfigureAwait(false);

        // Act
        Result<IReadOnlyList<CurrencyDto>> result = await _sut.ListAsync(true, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }
}
