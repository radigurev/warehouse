using FluentAssertions;
using FluentValidation.Results;
using Warehouse.Inventory.API.Validators;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for CreateAdjustmentRequest validation rules per SDD-INV-002.
/// </summary>
[TestFixture]
[Category("SDD-INV-002")]
public sealed class CreateAdjustmentRequestValidatorTests
{
    private CreateAdjustmentRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateAdjustmentRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        // Arrange
        CreateAdjustmentRequest request = new()
        {
            WarehouseId = 1,
            Reason = "Cycle count",
            Lines = [new CreateAdjustmentLineRequest { ProductId = 1, ActualQuantity = 95m }]
        };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_ZeroWarehouseId_Fails()
    {
        // Arrange
        CreateAdjustmentRequest request = new()
        {
            WarehouseId = 0,
            Reason = "Test",
            Lines = [new CreateAdjustmentLineRequest { ProductId = 1, ActualQuantity = 10m }]
        };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WarehouseId" && e.ErrorCode == "INVALID_WAREHOUSE");
    }

    [Test]
    public void Validate_EmptyReason_Fails()
    {
        // Arrange
        CreateAdjustmentRequest request = new()
        {
            WarehouseId = 1,
            Reason = "",
            Lines = [new CreateAdjustmentLineRequest { ProductId = 1, ActualQuantity = 10m }]
        };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason" && e.ErrorCode == "INVALID_ADJUSTMENT_REASON");
    }

    [Test]
    public void Validate_EmptyLines_Fails()
    {
        // Arrange
        CreateAdjustmentRequest request = new()
        {
            WarehouseId = 1,
            Reason = "Test",
            Lines = []
        };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines" && e.ErrorCode == "ADJUSTMENT_LINES_REQUIRED");
    }

    [Test]
    public void Validate_LineWithZeroProductId_Fails()
    {
        // Arrange
        CreateAdjustmentRequest request = new()
        {
            WarehouseId = 1,
            Reason = "Test",
            Lines = [new CreateAdjustmentLineRequest { ProductId = 0, ActualQuantity = 10m }]
        };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_PRODUCT");
    }
}
