using FluentAssertions;
using FluentValidation.Results;
using Warehouse.Customers.API.Validators;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for CreateAccountRequest validation rules per SDD-CUST-001 section 3.2.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CreateAccountRequestValidatorTests
{
    private CreateAccountRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateAccountRequestValidator();
    }

    [Test]
    public void CreateAccountRequestValidator_MissingCurrencyCode_Fails()
    {
        // Arrange
        CreateAccountRequest request = new() { CurrencyCode = "" };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }

    [Test]
    public void CreateAccountRequestValidator_InvalidCurrencyCode_Fails()
    {
        // Arrange
        CreateAccountRequest request = new() { CurrencyCode = "us" };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }
}
