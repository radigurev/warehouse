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
/// Unit tests for state/province operations: CRUD, parent validation, cascade deactivation, and uniqueness checks.
/// <para>See <see cref="StateProvinceService"/>.</para>
/// </summary>
[TestFixture]
[Category("SDD-NOM-001")]
public sealed class StateProvinceServiceTests : NomenclatureTestBase
{
    private Mock<IDistributedCache> _mockCache = null!;
    private StateProvinceService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockCache = new Mock<IDistributedCache>();
        _sut = new StateProvinceService(Context, Mapper, _mockCache.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedStateProvince()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        CreateStateProvinceRequest request = new()
        {
            CountryId = country.Id,
            Code = "SOF",
            Name = "Sofia Province"
        };

        // Act
        Result<StateProvinceDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.CountryId.Should().Be(country.Id);
        result.Value.Code.Should().Be("SOF");
        result.Value.Name.Should().Be("Sofia Province");
        result.Value.IsActive.Should().BeTrue();
        result.Value.Id.Should().BeGreaterThan(0);
        result.Value.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task CreateAsync_NonExistentCountry_ReturnsNotFoundError()
    {
        // Arrange
        CreateStateProvinceRequest request = new()
        {
            CountryId = 999,
            Code = "SOF",
            Name = "Sofia Province"
        };

        // Act
        Result<StateProvinceDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("COUNTRY_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task CreateAsync_InactiveCountry_ReturnsConflictError()
    {
        // Arrange
        Country country = await SeedCountryAsync(isActive: false).ConfigureAwait(false);
        CreateStateProvinceRequest request = new()
        {
            CountryId = country.Id,
            Code = "SOF",
            Name = "Sofia Province"
        };

        // Act
        Result<StateProvinceDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INACTIVE_PARENT_COUNTRY");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_DuplicateCodeWithinCountry_ReturnsConflictError()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        await SeedStateProvinceAsync(country.Id, code: "SOF", name: "Sofia Province").ConfigureAwait(false);
        CreateStateProvinceRequest request = new()
        {
            CountryId = country.Id,
            Code = "SOF",
            Name = "Sofia City Province"
        };

        // Act
        Result<StateProvinceDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_STATE_PROVINCE_CODE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_DuplicateCodeDifferentCountry_Succeeds()
    {
        // Arrange
        Country countryBg = await SeedCountryAsync(iso2Code: "BG", iso3Code: "BGR", name: "Bulgaria").ConfigureAwait(false);
        Country countryDe = await SeedCountryAsync(iso2Code: "DE", iso3Code: "DEU", name: "Germany").ConfigureAwait(false);
        await SeedStateProvinceAsync(countryBg.Id, code: "BY", name: "Burgas Province").ConfigureAwait(false);
        CreateStateProvinceRequest request = new()
        {
            CountryId = countryDe.Id,
            Code = "BY",
            Name = "Bavaria"
        };

        // Act
        Result<StateProvinceDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("BY");
        result.Value.CountryId.Should().Be(countryDe.Id);
    }

    [Test]
    public async Task UpdateAsync_ExistingStateProvince_ReturnsUpdatedStateProvince()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id, code: "SOF", name: "Sofia Province").ConfigureAwait(false);
        UpdateStateProvinceRequest request = new() { Name = "Sofia Region" };

        // Act
        Result<StateProvinceDto> result = await _sut.UpdateAsync(stateProvince.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Sofia Region");
        result.Value.ModifiedAtUtc.Should().NotBeNull();
        result.Value.ModifiedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task UpdateAsync_NonExistentStateProvince_ReturnsNotFoundError()
    {
        // Arrange
        UpdateStateProvinceRequest request = new() { Name = "Updated Name" };

        // Act
        Result<StateProvinceDto> result = await _sut.UpdateAsync(999, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("STATE_PROVINCE_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task DeactivateAsync_ActiveStateProvince_SetsInactive()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id).ConfigureAwait(false);

        // Act
        Result<StateProvinceDto> result = await _sut.DeactivateAsync(stateProvince.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();
        result.Value.ModifiedAtUtc.Should().NotBeNull();
    }

    [Test]
    public async Task DeactivateAsync_AlreadyInactive_ReturnsConflictError()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id, isActive: false).ConfigureAwait(false);

        // Act
        Result<StateProvinceDto> result = await _sut.DeactivateAsync(stateProvince.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("STATE_PROVINCE_ALREADY_INACTIVE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeactivateAsync_CascadesDeactivationToCities()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id).ConfigureAwait(false);
        City city = await SeedCityAsync(stateProvince.Id, name: "Sofia", postalCode: "1000").ConfigureAwait(false);

        // Act
        Result<StateProvinceDto> result = await _sut.DeactivateAsync(stateProvince.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();

        await Context.Entry(city).ReloadAsync(CancellationToken.None).ConfigureAwait(false);
        city.IsActive.Should().BeFalse();
        city.ModifiedAtUtc.Should().NotBeNull();
    }

    [Test]
    public async Task ReactivateAsync_InactiveStateProvince_SetsActive()
    {
        // Arrange
        Country country = await SeedCountryAsync(isActive: true).ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id, isActive: false).ConfigureAwait(false);

        // Act
        Result<StateProvinceDto> result = await _sut.ReactivateAsync(stateProvince.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeTrue();
        result.Value.ModifiedAtUtc.Should().NotBeNull();
    }

    [Test]
    public async Task ReactivateAsync_AlreadyActive_ReturnsConflictError()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id, isActive: true).ConfigureAwait(false);

        // Act
        Result<StateProvinceDto> result = await _sut.ReactivateAsync(stateProvince.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("STATE_PROVINCE_ALREADY_ACTIVE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task ReactivateAsync_InactiveParentCountry_ReturnsConflictError()
    {
        // Arrange
        Country country = await SeedCountryAsync(isActive: false).ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id, isActive: false).ConfigureAwait(false);

        // Act
        Result<StateProvinceDto> result = await _sut.ReactivateAsync(stateProvince.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INACTIVE_PARENT_COUNTRY");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task ListByCountryAsync_ReturnsActiveStateProvincesSortedByName()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        await SeedStateProvinceAsync(country.Id, code: "PV", name: "Plovdiv Province").ConfigureAwait(false);
        await SeedStateProvinceAsync(country.Id, code: "BU", name: "Burgas Province").ConfigureAwait(false);
        await SeedStateProvinceAsync(country.Id, code: "VT", name: "Veliko Tarnovo", isActive: false).ConfigureAwait(false);

        // Act
        Result<IReadOnlyList<StateProvinceDto>> result = await _sut.ListByCountryAsync(country.Id, includeInactive: false, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        IReadOnlyList<StateProvinceDto> items = result.Value!;
        items.Should().HaveCount(2);
        items[0].Name.Should().Be("Burgas Province");
        items[1].Name.Should().Be("Plovdiv Province");
    }

    [Test]
    public async Task ListByCountryAsync_NonExistentCountry_ReturnsNotFoundError()
    {
        // Arrange — no seed needed

        // Act
        Result<IReadOnlyList<StateProvinceDto>> result = await _sut.ListByCountryAsync(999, includeInactive: false, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("COUNTRY_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task ListByCountryAsync_IncludeInactive_ReturnsAll()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        await SeedStateProvinceAsync(country.Id, code: "PV", name: "Plovdiv Province").ConfigureAwait(false);
        await SeedStateProvinceAsync(country.Id, code: "BU", name: "Burgas Province").ConfigureAwait(false);
        await SeedStateProvinceAsync(country.Id, code: "VT", name: "Veliko Tarnovo", isActive: false).ConfigureAwait(false);

        // Act
        Result<IReadOnlyList<StateProvinceDto>> result = await _sut.ListByCountryAsync(country.Id, includeInactive: true, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(3);
    }
}
