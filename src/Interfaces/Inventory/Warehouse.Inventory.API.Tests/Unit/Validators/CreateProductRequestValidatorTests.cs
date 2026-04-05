using FluentAssertions;
using FluentValidation.Results;
using Warehouse.Inventory.API.Validators;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for CreateProductRequest validation rules per SDD-INV-001 section 3.
/// </summary>
[TestFixture]
[Category("SDD-INV-001")]
public sealed class CreateProductRequestValidatorTests
{
    private CreateProductRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateProductRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        // Arrange
        CreateProductRequest request = new() { Code = "PROD-001", Name = "Test Product", UnitOfMeasureId = 1 };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyCode_Fails()
    {
        // Arrange
        CreateProductRequest request = new() { Code = "", Name = "Test", UnitOfMeasureId = 1 };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code" && e.ErrorCode == "INVALID_PRODUCT_CODE");
    }

    [Test]
    public void Validate_CodeTooLong_Fails()
    {
        // Arrange
        CreateProductRequest request = new() { Code = new string('X', 51), Name = "Test", UnitOfMeasureId = 1 };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Test]
    public void Validate_CodeWithInvalidCharacters_Fails()
    {
        // Arrange
        CreateProductRequest request = new() { Code = "INVALID@CODE!", Name = "Test", UnitOfMeasureId = 1 };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code" && e.ErrorCode == "INVALID_PRODUCT_CODE");
    }

    [Test]
    public void Validate_EmptyName_Fails()
    {
        // Arrange
        CreateProductRequest request = new() { Code = "PROD-001", Name = "", UnitOfMeasureId = 1 };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorCode == "INVALID_PRODUCT_NAME");
    }

    [Test]
    public void Validate_ZeroUnitOfMeasureId_Fails()
    {
        // Arrange
        CreateProductRequest request = new() { Code = "PROD-001", Name = "Test", UnitOfMeasureId = 0 };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UnitOfMeasureId" && e.ErrorCode == "INVALID_UNIT_OF_MEASURE");
    }
}
