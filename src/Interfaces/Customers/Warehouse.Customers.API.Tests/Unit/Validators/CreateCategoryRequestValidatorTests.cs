using FluentAssertions;
using FluentValidation.Results;
using Warehouse.Customers.API.Validators;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for CreateCategoryRequest validation rules per SDD-CUST-001 section 3.6.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CreateCategoryRequestValidatorTests
{
    private CreateCategoryRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateCategoryRequestValidator();
    }

    [Test]
    public void CreateCategoryRequestValidator_MissingName_Fails()
    {
        // Arrange
        CreateCategoryRequest request = new() { Name = "" };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Test]
    public void CreateCategoryRequestValidator_NameTooLong_Fails()
    {
        // Arrange
        CreateCategoryRequest request = new() { Name = new string('A', 101) };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}
