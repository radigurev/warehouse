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
/// Unit tests for country CRUD, deactivation cascade, and reactivation per SDD-NOM-001 section 7.1.
/// <para>Links to specification SDD-NOM-001.</para>
/// </summary>
[TestFixture]
[Category("SDD-NOM-001")]
public sealed class CountryServiceTests : NomenclatureTestBase
{
    private Mock<IDistributedCache> _mockCache = null!;
    private CountryService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockCache = new Mock<IDistributedCache>();
        _sut = new CountryService(Context, Mapper, _mockCache.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedCountry()
    {
        // Arrange
        CreateCountryRequest request = new()
        {
            Iso2Code = "BG",
            Iso3Code = "BGR",
            Name = "Bulgaria",
            PhonePrefix = "+359"
        };

        // Act
        Result<CountryDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Iso2Code.Should().Be("BG");
        result.Value.Iso3Code.Should().Be("BGR");
        result.Value.Name.Should().Be("Bulgaria");
        result.Value.PhonePrefix.Should().Be("+359");
        result.Value.IsActive.Should().BeTrue();
        result.Value.Id.Should().BeGreaterThan(0);
        result.Value.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task CreateAsync_DuplicateIso2Code_ReturnsConflictError()
    {
        // Arrange
        await SeedCountryAsync(iso2Code: "BG", iso3Code: "BGR").ConfigureAwait(false);
        CreateCountryRequest request = new()
        {
            Iso2Code = "BG",
            Iso3Code = "XXX",
            Name = "Duplicate ISO2"
        };

        // Act
        Result<CountryDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_ISO2_CODE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_DuplicateIso3Code_ReturnsConflictError()
    {
        // Arrange
        await SeedCountryAsync(iso2Code: "BG", iso3Code: "BGR").ConfigureAwait(false);
        CreateCountryRequest request = new()
        {
            Iso2Code = "XX",
            Iso3Code = "BGR",
            Name = "Duplicate ISO3"
        };

        // Act
        Result<CountryDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_ISO3_CODE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task UpdateAsync_ExistingCountry_ReturnsUpdatedCountry()
    {
        // Arrange
        Country seeded = await SeedCountryAsync(name: "Bulgaria", phonePrefix: "+359").ConfigureAwait(false);
        UpdateCountryRequest request = new()
        {
            Name = "Republic of Bulgaria",
            PhonePrefix = "+00359"
        };

        // Act
        Result<CountryDto> result = await _sut.UpdateAsync(seeded.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Republic of Bulgaria");
        result.Value.PhonePrefix.Should().Be("+00359");
        result.Value.ModifiedAtUtc.Should().NotBeNull();
        result.Value.ModifiedAtUtc!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task UpdateAsync_NonExistentCountry_ReturnsNotFoundError()
    {
        // Arrange
        UpdateCountryRequest request = new() { Name = "Nowhere" };

        // Act
        Result<CountryDto> result = await _sut.UpdateAsync(999, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("COUNTRY_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task DeactivateAsync_ActiveCountry_SetsInactive()
    {
        // Arrange
        Country seeded = await SeedCountryAsync(isActive: true).ConfigureAwait(false);

        // Act
        Result<CountryDto> result = await _sut.DeactivateAsync(seeded.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task DeactivateAsync_AlreadyInactive_ReturnsConflictError()
    {
        // Arrange
        Country seeded = await SeedCountryAsync(isActive: false).ConfigureAwait(false);

        // Act
        Result<CountryDto> result = await _sut.DeactivateAsync(seeded.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("COUNTRY_ALREADY_INACTIVE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeactivateAsync_CascadesDeactivationToStateProvinces()
    {
        // Arrange
        Country country = await SeedCountryAsync(isActive: true).ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id, code: "SOF", name: "Sofia Province", isActive: true).ConfigureAwait(false);
        City city = await SeedCityAsync(stateProvince.Id, name: "Sofia", postalCode: "1000", isActive: true).ConfigureAwait(false);

        // Act
        Result<CountryDto> result = await _sut.DeactivateAsync(country.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await Context.Entry(stateProvince).ReloadAsync(CancellationToken.None).ConfigureAwait(false);
        await Context.Entry(city).ReloadAsync(CancellationToken.None).ConfigureAwait(false);

        stateProvince.IsActive.Should().BeFalse();
        city.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task ReactivateAsync_InactiveCountry_SetsActive()
    {
        // Arrange
        Country seeded = await SeedCountryAsync(isActive: false).ConfigureAwait(false);

        // Act
        Result<CountryDto> result = await _sut.ReactivateAsync(seeded.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task ReactivateAsync_AlreadyActive_ReturnsConflictError()
    {
        // Arrange
        Country seeded = await SeedCountryAsync(isActive: true).ConfigureAwait(false);

        // Act
        Result<CountryDto> result = await _sut.ReactivateAsync(seeded.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("COUNTRY_ALREADY_ACTIVE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task ReactivateAsync_DoesNotReactivateChildren()
    {
        // Arrange
        Country country = await SeedCountryAsync(isActive: false).ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id, code: "SOF", name: "Sofia Province", isActive: false).ConfigureAwait(false);

        // Act
        Result<CountryDto> result = await _sut.ReactivateAsync(country.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeTrue();

        await Context.Entry(stateProvince).ReloadAsync(CancellationToken.None).ConfigureAwait(false);
        stateProvince.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task GetByIdAsync_ExistingCountry_ReturnsCountryWithStateProvinces()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        await SeedStateProvinceAsync(country.Id, code: "SOF", name: "Sofia Province").ConfigureAwait(false);

        // Act
        Result<CountryDetailDto> result = await _sut.GetByIdAsync(country.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(country.Id);
        result.Value.Name.Should().Be("Bulgaria");
        result.Value.StateProvinces.Should().HaveCount(1);
        result.Value.StateProvinces[0].Code.Should().Be("SOF");
    }

    [Test]
    public async Task GetByIdAsync_NonExistentCountry_ReturnsNotFoundError()
    {
        // Arrange — no seeding

        // Act
        Result<CountryDetailDto> result = await _sut.GetByIdAsync(999, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("COUNTRY_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task ListAsync_ReturnsActiveCountriesSortedByName()
    {
        // Arrange
        await SeedCountryAsync(iso2Code: "US", iso3Code: "USA", name: "United States", isActive: true).ConfigureAwait(false);
        await SeedCountryAsync(iso2Code: "BG", iso3Code: "BGR", name: "Bulgaria", isActive: true).ConfigureAwait(false);
        await SeedCountryAsync(iso2Code: "DE", iso3Code: "DEU", name: "Germany", isActive: false).ConfigureAwait(false);

        // Act
        Result<IReadOnlyList<CountryDto>> result = await _sut.ListAsync(false, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Name.Should().Be("Bulgaria");
        result.Value[1].Name.Should().Be("United States");
    }

    [Test]
    public async Task ListAsync_IncludeInactive_ReturnsAllCountries()
    {
        // Arrange
        await SeedCountryAsync(iso2Code: "US", iso3Code: "USA", name: "United States", isActive: true).ConfigureAwait(false);
        await SeedCountryAsync(iso2Code: "BG", iso3Code: "BGR", name: "Bulgaria", isActive: true).ConfigureAwait(false);
        await SeedCountryAsync(iso2Code: "DE", iso3Code: "DEU", name: "Germany", isActive: false).ConfigureAwait(false);

        // Act
        Result<IReadOnlyList<CountryDto>> result = await _sut.ListAsync(true, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }
}
