using FluentAssertions;
using FluentValidation.Results;
using Microsoft.FeatureManagement;
using Moq;
using Warehouse.Customers.API.Validators;
using Warehouse.Infrastructure.Caching;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for CreateAddressRequest validation rules per SDD-CUST-001 section 3.3.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CreateAddressRequestValidatorTests
{
    private Mock<INomenclatureResolver> _nomenclatureResolverMock = null!;
    private Mock<IFeatureManager> _featureManagerMock = null!;
    private CreateAddressRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _nomenclatureResolverMock = new Mock<INomenclatureResolver>();
        _featureManagerMock = new Mock<IFeatureManager>();
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _validator = new CreateAddressRequestValidator(
            _nomenclatureResolverMock.Object,
            _featureManagerMock.Object);
    }

    [Test]
    public async Task CreateAddressRequestValidator_MissingRequiredFields_Fails()
    {
        // Arrange
        CreateAddressRequest request = new()
        {
            AddressType = "",
            StreetLine1 = "",
            City = "",
            PostalCode = "",
            CountryCode = ""
        };

        // Act
        ValidationResult result = await _validator.ValidateAsync(request).ConfigureAwait(false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StreetLine1");
        result.Errors.Should().Contain(e => e.PropertyName == "City");
        result.Errors.Should().Contain(e => e.PropertyName == "PostalCode");
        result.Errors.Should().Contain(e => e.PropertyName == "CountryCode");
    }

    [Test]
    public async Task CreateAddressRequestValidator_InvalidAddressType_Fails()
    {
        // Arrange
        CreateAddressRequest request = new()
        {
            AddressType = "InvalidType",
            StreetLine1 = "123 Main St",
            City = "Sofia",
            PostalCode = "1000",
            CountryCode = "BG"
        };

        // Act
        ValidationResult result = await _validator.ValidateAsync(request).ConfigureAwait(false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AddressType");
    }

    [Test]
    public async Task CreateAddressRequestValidator_InvalidCountryCode_Fails()
    {
        // Arrange
        CreateAddressRequest request = new()
        {
            AddressType = "Billing",
            StreetLine1 = "123 Main St",
            City = "Sofia",
            PostalCode = "1000",
            CountryCode = "bg"
        };

        // Act
        ValidationResult result = await _validator.ValidateAsync(request).ConfigureAwait(false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CountryCode");
    }
}
