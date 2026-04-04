using FluentAssertions;
using FluentValidation.Results;
using Warehouse.Customers.API.Validators;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for CreateCustomerRequest validation rules per SDD-CUST-001 section 3.1.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CreateCustomerRequestValidatorTests
{
    private CreateCustomerRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateCustomerRequestValidator();
    }

    [Test]
    public void CreateCustomerRequestValidator_MissingName_Fails()
    {
        // Arrange
        CreateCustomerRequest request = new() { Name = "" };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Test]
    public void CreateCustomerRequestValidator_NameTooLong_Fails()
    {
        // Arrange
        CreateCustomerRequest request = new() { Name = new string('A', 201) };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Test]
    public void CreateCustomerRequestValidator_CodeTooLong_Fails()
    {
        // Arrange
        CreateCustomerRequest request = new() { Name = "Valid Name", Code = new string('X', 21) };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Test]
    public void CreateCustomerRequestValidator_InvalidCodeCharacters_Fails()
    {
        // Arrange
        CreateCustomerRequest request = new() { Name = "Valid Name", Code = "INVALID@CODE!" };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }
}
