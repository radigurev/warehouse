using FluentAssertions;
using FluentValidation.Results;
using Warehouse.Customers.API.Validators;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for CreateAddressRequest validation rules per SDD-CUST-001 section 3.3.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CreateAddressRequestValidatorTests
{
    private CreateAddressRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateAddressRequestValidator();
    }

    [Test]
    public void CreateAddressRequestValidator_MissingRequiredFields_Fails()
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
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StreetLine1");
        result.Errors.Should().Contain(e => e.PropertyName == "City");
        result.Errors.Should().Contain(e => e.PropertyName == "PostalCode");
        result.Errors.Should().Contain(e => e.PropertyName == "CountryCode");
    }

    [Test]
    public void CreateAddressRequestValidator_InvalidAddressType_Fails()
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
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AddressType");
    }

    [Test]
    public void CreateAddressRequestValidator_InvalidCountryCode_Fails()
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
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CountryCode");
    }
}
