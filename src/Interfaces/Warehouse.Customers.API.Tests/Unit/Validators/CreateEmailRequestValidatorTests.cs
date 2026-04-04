using FluentAssertions;
using FluentValidation.Results;
using Warehouse.Customers.API.Validators;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for CreateEmailRequest validation rules per SDD-CUST-001 section 3.5.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CreateEmailRequestValidatorTests
{
    private CreateEmailRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateEmailRequestValidator();
    }

    [Test]
    public void CreateEmailRequestValidator_InvalidEmailFormat_Fails()
    {
        // Arrange
        CreateEmailRequest request = new()
        {
            EmailType = "General",
            EmailAddress = "not-an-email"
        };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmailAddress");
    }

    [Test]
    public void CreateEmailRequestValidator_InvalidEmailType_Fails()
    {
        // Arrange
        CreateEmailRequest request = new()
        {
            EmailType = "Personal",
            EmailAddress = "test@example.com"
        };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmailType");
    }
}
