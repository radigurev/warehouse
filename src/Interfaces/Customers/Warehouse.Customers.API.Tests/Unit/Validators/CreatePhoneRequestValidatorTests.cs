using FluentAssertions;
using FluentValidation.Results;
using Warehouse.Customers.API.Validators;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for CreatePhoneRequest validation rules per SDD-CUST-001 section 3.4.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CreatePhoneRequestValidatorTests
{
    private CreatePhoneRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreatePhoneRequestValidator();
    }

    [Test]
    public void CreatePhoneRequestValidator_InvalidPhoneNumber_Fails()
    {
        // Arrange
        CreatePhoneRequest request = new()
        {
            PhoneType = "Mobile",
            PhoneNumber = "123"
        };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Test]
    public void CreatePhoneRequestValidator_InvalidPhoneType_Fails()
    {
        // Arrange
        CreatePhoneRequest request = new()
        {
            PhoneType = "Satellite",
            PhoneNumber = "+359888123456"
        };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneType");
    }
}
