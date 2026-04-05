using FluentAssertions;
using FluentValidation.Results;
using Warehouse.Inventory.API.Validators;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for RecordStockMovementRequest validation rules per SDD-INV-002.
/// </summary>
[TestFixture]
[Category("SDD-INV-002")]
public sealed class RecordStockMovementRequestValidatorTests
{
    private RecordStockMovementRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new RecordStockMovementRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        // Arrange
        RecordStockMovementRequest request = new()
        {
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 10m,
            ReasonCode = "Receipt"
        };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_ZeroProductId_Fails()
    {
        // Arrange
        RecordStockMovementRequest request = new()
        {
            ProductId = 0,
            WarehouseId = 1,
            Quantity = 10m,
            ReasonCode = "Receipt"
        };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId" && e.ErrorCode == "INVALID_PRODUCT");
    }

    [Test]
    public void Validate_ZeroQuantity_Fails()
    {
        // Arrange
        RecordStockMovementRequest request = new()
        {
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 0m,
            ReasonCode = "Receipt"
        };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity" && e.ErrorCode == "INVALID_MOVEMENT_QUANTITY");
    }

    [Test]
    public void Validate_EmptyReasonCode_Fails()
    {
        // Arrange
        RecordStockMovementRequest request = new()
        {
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 10m,
            ReasonCode = ""
        };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReasonCode" && e.ErrorCode == "INVALID_REASON_CODE");
    }

    [Test]
    public void Validate_NegativeQuantity_Passes()
    {
        // Arrange
        RecordStockMovementRequest request = new()
        {
            ProductId = 1,
            WarehouseId = 1,
            Quantity = -5m,
            ReasonCode = "Shrinkage"
        };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
