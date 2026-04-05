using FluentAssertions;
using FluentValidation.Results;
using Warehouse.Inventory.API.Validators;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for CreateZoneRequest validation rules per SDD-INV-003.
/// </summary>
[TestFixture]
[Category("SDD-INV-003")]
public sealed class CreateZoneRequestValidatorTests
{
    private CreateZoneRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateZoneRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        // Arrange
        CreateZoneRequest request = new() { WarehouseId = 1, Code = "Z-A", Name = "Zone A" };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_ZeroWarehouseId_Fails()
    {
        // Arrange
        CreateZoneRequest request = new() { WarehouseId = 0, Code = "Z-A", Name = "Zone A" };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WarehouseId" && e.ErrorCode == "INVALID_WAREHOUSE_ID");
    }

    [Test]
    public void Validate_EmptyCode_Fails()
    {
        // Arrange
        CreateZoneRequest request = new() { WarehouseId = 1, Code = "", Name = "Zone A" };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code" && e.ErrorCode == "INVALID_ZONE_CODE");
    }

    [Test]
    public void Validate_EmptyName_Fails()
    {
        // Arrange
        CreateZoneRequest request = new() { WarehouseId = 1, Code = "Z-A", Name = "" };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorCode == "INVALID_ZONE_NAME");
    }
}
