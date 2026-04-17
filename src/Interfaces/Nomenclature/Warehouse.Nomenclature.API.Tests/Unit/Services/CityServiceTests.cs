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
/// Unit tests for city operations: CRUD, parent validation, uniqueness checks, and soft-delete.
/// <para>See <see cref="CityService"/>.</para>
/// </summary>
[TestFixture]
[Category("SDD-NOM-001")]
public sealed class CityServiceTests : NomenclatureTestBase
{
    private Mock<IDistributedCache> _mockCache = null!;
    private CityService _sut = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _mockCache = new Mock<IDistributedCache>();
        _sut = new CityService(Context, Mapper, _mockCache.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedCity()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id).ConfigureAwait(false);
        CreateCityRequest request = new()
        {
            StateProvinceId = stateProvince.Id,
            Name = "Sofia",
            PostalCode = "1000"
        };

        // Act
        Result<CityDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StateProvinceId.Should().Be(stateProvince.Id);
        result.Value.Name.Should().Be("Sofia");
        result.Value.PostalCode.Should().Be("1000");
        result.Value.IsActive.Should().BeTrue();
        result.Value.Id.Should().BeGreaterThan(0);
        result.Value.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task CreateAsync_NonExistentStateProvince_ReturnsNotFoundError()
    {
        // Arrange
        CreateCityRequest request = new()
        {
            StateProvinceId = 999,
            Name = "Sofia",
            PostalCode = "1000"
        };

        // Act
        Result<CityDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("STATE_PROVINCE_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task CreateAsync_InactiveStateProvince_ReturnsConflictError()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id, isActive: false).ConfigureAwait(false);
        CreateCityRequest request = new()
        {
            StateProvinceId = stateProvince.Id,
            Name = "Sofia",
            PostalCode = "1000"
        };

        // Act
        Result<CityDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INACTIVE_PARENT_STATE_PROVINCE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_DuplicateNameWithinStateProvince_ReturnsConflictError()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id).ConfigureAwait(false);
        await SeedCityAsync(stateProvince.Id, name: "Sofia").ConfigureAwait(false);
        CreateCityRequest request = new()
        {
            StateProvinceId = stateProvince.Id,
            Name = "Sofia",
            PostalCode = "1001"
        };

        // Act
        Result<CityDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CITY_NAME");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateAsync_DuplicateNameDifferentStateProvince_Succeeds()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        StateProvince sp1 = await SeedStateProvinceAsync(country.Id, code: "SOF", name: "Sofia Province").ConfigureAwait(false);
        StateProvince sp2 = await SeedStateProvinceAsync(country.Id, code: "PV", name: "Plovdiv Province").ConfigureAwait(false);
        await SeedCityAsync(sp1.Id, name: "Kazanlak").ConfigureAwait(false);
        CreateCityRequest request = new()
        {
            StateProvinceId = sp2.Id,
            Name = "Kazanlak",
            PostalCode = "6100"
        };

        // Act
        Result<CityDto> result = await _sut.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Kazanlak");
        result.Value.StateProvinceId.Should().Be(sp2.Id);
    }

    [Test]
    public async Task UpdateAsync_ExistingCity_ReturnsUpdatedCity()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id).ConfigureAwait(false);
        City city = await SeedCityAsync(stateProvince.Id, name: "Sofia", postalCode: "1000").ConfigureAwait(false);
        UpdateCityRequest request = new() { Name = "Sofia City", PostalCode = "1001" };

        // Act
        Result<CityDto> result = await _sut.UpdateAsync(city.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Sofia City");
        result.Value.PostalCode.Should().Be("1001");
        result.Value.ModifiedAtUtc.Should().NotBeNull();
        result.Value.ModifiedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task UpdateAsync_NonExistentCity_ReturnsNotFoundError()
    {
        // Arrange
        UpdateCityRequest request = new() { Name = "Updated Name", PostalCode = "0000" };

        // Act
        Result<CityDto> result = await _sut.UpdateAsync(999, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CITY_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task UpdateAsync_DuplicateNameWithinStateProvince_ReturnsConflictError()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id).ConfigureAwait(false);
        await SeedCityAsync(stateProvince.Id, name: "Sofia").ConfigureAwait(false);
        City plovdiv = await SeedCityAsync(stateProvince.Id, name: "Plovdiv").ConfigureAwait(false);
        UpdateCityRequest request = new() { Name = "Sofia", PostalCode = "4000" };

        // Act
        Result<CityDto> result = await _sut.UpdateAsync(plovdiv.Id, request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CITY_NAME");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task DeactivateAsync_ActiveCity_SetsInactive()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id).ConfigureAwait(false);
        City city = await SeedCityAsync(stateProvince.Id, name: "Sofia").ConfigureAwait(false);

        // Act
        Result<CityDto> result = await _sut.DeactivateAsync(city.Id, CancellationToken.None).ConfigureAwait(false);

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
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id).ConfigureAwait(false);
        City city = await SeedCityAsync(stateProvince.Id, name: "Sofia", isActive: false).ConfigureAwait(false);

        // Act
        Result<CityDto> result = await _sut.DeactivateAsync(city.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CITY_ALREADY_INACTIVE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task ReactivateAsync_InactiveCity_SetsActive()
    {
        // Arrange
        Country country = await SeedCountryAsync(isActive: true).ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id, isActive: true).ConfigureAwait(false);
        City city = await SeedCityAsync(stateProvince.Id, name: "Sofia", isActive: false).ConfigureAwait(false);

        // Act
        Result<CityDto> result = await _sut.ReactivateAsync(city.Id, CancellationToken.None).ConfigureAwait(false);

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
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id).ConfigureAwait(false);
        City city = await SeedCityAsync(stateProvince.Id, name: "Sofia", isActive: true).ConfigureAwait(false);

        // Act
        Result<CityDto> result = await _sut.ReactivateAsync(city.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CITY_ALREADY_ACTIVE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task ReactivateAsync_InactiveParentStateProvince_ReturnsConflictError()
    {
        // Arrange
        Country country = await SeedCountryAsync(isActive: true).ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id, isActive: false).ConfigureAwait(false);
        City city = await SeedCityAsync(stateProvince.Id, name: "Sofia", isActive: false).ConfigureAwait(false);

        // Act
        Result<CityDto> result = await _sut.ReactivateAsync(city.Id, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INACTIVE_PARENT_STATE_PROVINCE");
        result.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task ListByStateProvinceAsync_ReturnsActiveCitiesSortedByName()
    {
        // Arrange
        Country country = await SeedCountryAsync().ConfigureAwait(false);
        StateProvince stateProvince = await SeedStateProvinceAsync(country.Id).ConfigureAwait(false);
        await SeedCityAsync(stateProvince.Id, name: "Plovdiv", postalCode: "4000").ConfigureAwait(false);
        await SeedCityAsync(stateProvince.Id, name: "Burgas", postalCode: "8000").ConfigureAwait(false);
        await SeedCityAsync(stateProvince.Id, name: "Varna", postalCode: "9000", isActive: false).ConfigureAwait(false);

        // Act
        Result<IReadOnlyList<CityDto>> result = await _sut.ListByStateProvinceAsync(stateProvince.Id, includeInactive: false, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        IReadOnlyList<CityDto> items = result.Value!;
        items.Should().HaveCount(2);
        items[0].Name.Should().Be("Burgas");
        items[1].Name.Should().Be("Plovdiv");
    }

    [Test]
    public async Task ListByStateProvinceAsync_NonExistentStateProvince_ReturnsNotFoundError()
    {
        // Arrange — no seed needed

        // Act
        Result<IReadOnlyList<CityDto>> result = await _sut.ListByStateProvinceAsync(999, includeInactive: false, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("STATE_PROVINCE_NOT_FOUND");
        result.StatusCode.Should().Be(404);
    }
}
