using FluentAssertions;
using FluentValidation.Results;
using Warehouse.Inventory.API.Validators;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for CreateWarehouseRequest validation rules per SDD-INV-003 section 3.
/// </summary>
[TestFixture]
[Category("SDD-INV-003")]
public sealed class CreateWarehouseRequestValidatorTests
{
    private CreateWarehouseRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateWarehouseRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        // Arrange
        CreateWarehouseRequest request = new() { Code = "WH-001", Name = "Main Warehouse" };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyCode_Fails()
    {
        // Arrange
        CreateWarehouseRequest request = new() { Code = "", Name = "Warehouse" };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code" && e.ErrorCode == "INVALID_WAREHOUSE_CODE");
    }

    [Test]
    public void Validate_CodeTooLong_Fails()
    {
        // Arrange
        CreateWarehouseRequest request = new() { Code = new string('W', 21), Name = "Warehouse" };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Test]
    public void Validate_EmptyName_Fails()
    {
        // Arrange
        CreateWarehouseRequest request = new() { Code = "WH-001", Name = "" };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorCode == "INVALID_WAREHOUSE_NAME");
    }

    [Test]
    public void Validate_CodeWithSpecialCharacters_Fails()
    {
        // Arrange
        CreateWarehouseRequest request = new() { Code = "WH@#$", Name = "Warehouse" };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code" && e.ErrorCode == "INVALID_WAREHOUSE_CODE");
    }
}
