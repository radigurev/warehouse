using FluentAssertions;
using FluentValidation.Results;
using Warehouse.Purchasing.API.Validators;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for CreateSupplierRequest validation rules per SDD-PURCH-001 section 3.1.
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class CreateSupplierRequestValidatorTests
{
    private CreateSupplierRequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new CreateSupplierRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_ReturnsValid()
    {
        // Arrange
        CreateSupplierRequest request = new() { Name = "Acme Supplies" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyName_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierRequest request = new() { Name = "" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_SUPPLIER_NAME");
    }

    [Test]
    public void Validate_NameTooLong_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierRequest request = new() { Name = new string('A', 201) };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_SUPPLIER_NAME");
    }

    [Test]
    public void Validate_InvalidCodeFormat_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierRequest request = new() { Name = "Acme", Code = "INVALID CODE!" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_SUPPLIER_CODE");
    }

    [Test]
    public void Validate_CodeTooLong_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierRequest request = new() { Name = "Acme", Code = new string('A', 21) };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_SUPPLIER_CODE");
    }

    [Test]
    public void Validate_TaxIdTooLong_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierRequest request = new() { Name = "Acme", TaxId = new string('T', 51) };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_TAX_ID");
    }

    [Test]
    public void Validate_NegativePaymentTermDays_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierRequest request = new() { Name = "Acme", PaymentTermDays = -1 };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_PAYMENT_TERMS");
    }

    [Test]
    public void Validate_PaymentTermDaysExceeds365_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierRequest request = new() { Name = "Acme", PaymentTermDays = 366 };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_PAYMENT_TERMS");
    }

    [Test]
    public void Validate_NotesTooLong_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierRequest request = new() { Name = "Acme", Notes = new string('N', 2001) };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_NOTES");
    }
}

/// <summary>
/// Unit tests for CreateSupplierCategoryRequest validation rules per SDD-PURCH-001 section 3.2.
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class CreateSupplierCategoryRequestValidatorTests
{
    private CreateSupplierCategoryRequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new CreateSupplierCategoryRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_ReturnsValid()
    {
        // Arrange
        CreateSupplierCategoryRequest request = new() { Name = "Electronics" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyName_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierCategoryRequest request = new() { Name = "" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_CATEGORY_NAME");
    }

    [Test]
    public void Validate_NameTooLong_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierCategoryRequest request = new() { Name = new string('C', 101) };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_CATEGORY_NAME");
    }

    [Test]
    public void Validate_DescriptionTooLong_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierCategoryRequest request = new() { Name = "Electronics", Description = new string('D', 501) };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_CATEGORY_DESCRIPTION");
    }
}

/// <summary>
/// Unit tests for CreatePurchaseOrderRequest validation rules per SDD-PURCH-001 section 3.6-3.7.
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class CreatePurchaseOrderRequestValidatorTests
{
    private CreatePurchaseOrderRequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new CreatePurchaseOrderRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_ReturnsValid()
    {
        // Arrange
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = 1,
            DestinationWarehouseId = 1,
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = 1, OrderedQuantity = 10, UnitPrice = 5.00m }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_ZeroSupplierId_ReturnsInvalid()
    {
        // Arrange
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = 0,
            DestinationWarehouseId = 1,
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = 1, OrderedQuantity = 10, UnitPrice = 5.00m }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_SUPPLIER");
    }

    [Test]
    public void Validate_ZeroWarehouseId_ReturnsInvalid()
    {
        // Arrange
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = 1,
            DestinationWarehouseId = 0,
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = 1, OrderedQuantity = 10, UnitPrice = 5.00m }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_WAREHOUSE");
    }

    [Test]
    public void Validate_PastDeliveryDate_ReturnsInvalid()
    {
        // Arrange
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = 1,
            DestinationWarehouseId = 1,
            ExpectedDeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)),
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = 1, OrderedQuantity = 10, UnitPrice = 5.00m }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_DELIVERY_DATE");
    }

    [Test]
    public void Validate_EmptyLines_ReturnsInvalid()
    {
        // Arrange
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = 1,
            DestinationWarehouseId = 1,
            Lines = []
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PO_MUST_HAVE_LINES");
    }

    [Test]
    public void Validate_ZeroProductIdInLine_ReturnsInvalid()
    {
        // Arrange
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = 1,
            DestinationWarehouseId = 1,
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = 0, OrderedQuantity = 10, UnitPrice = 5.00m }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_PRODUCT");
    }

    [Test]
    public void Validate_ZeroOrderedQuantityInLine_ReturnsInvalid()
    {
        // Arrange
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = 1,
            DestinationWarehouseId = 1,
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = 1, OrderedQuantity = 0, UnitPrice = 5.00m }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_QUANTITY");
    }

    [Test]
    public void Validate_NegativeUnitPriceInLine_ReturnsInvalid()
    {
        // Arrange
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = 1,
            DestinationWarehouseId = 1,
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = 1, OrderedQuantity = 10, UnitPrice = -1m }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_UNIT_PRICE");
    }

    [Test]
    public void Validate_LineNotesTooLong_ReturnsInvalid()
    {
        // Arrange
        CreatePurchaseOrderRequest request = new()
        {
            SupplierId = 1,
            DestinationWarehouseId = 1,
            Lines = [new CreatePurchaseOrderLineRequest { ProductId = 1, OrderedQuantity = 10, UnitPrice = 5.00m, Notes = new string('N', 501) }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_LINE_NOTES");
    }
}

/// <summary>
/// Unit tests for CreateGoodsReceiptRequest validation rules per SDD-PURCH-001 section 3.8-3.9.
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class CreateGoodsReceiptRequestValidatorTests
{
    private CreateGoodsReceiptRequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new CreateGoodsReceiptRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_ReturnsValid()
    {
        // Arrange
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = 1,
            WarehouseId = 1,
            Lines = [new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = 1, ReceivedQuantity = 10 }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_ZeroPurchaseOrderId_ReturnsInvalid()
    {
        // Arrange
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = 0,
            WarehouseId = 1,
            Lines = [new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = 1, ReceivedQuantity = 10 }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_PO");
    }

    [Test]
    public void Validate_ZeroWarehouseId_ReturnsInvalid()
    {
        // Arrange
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = 1,
            WarehouseId = 0,
            Lines = [new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = 1, ReceivedQuantity = 10 }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_WAREHOUSE");
    }

    [Test]
    public void Validate_EmptyLines_ReturnsInvalid()
    {
        // Arrange
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = 1,
            WarehouseId = 1,
            Lines = []
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "RECEIPT_MUST_HAVE_LINES");
    }

    [Test]
    public void Validate_ZeroPurchaseOrderLineIdInLine_ReturnsInvalid()
    {
        // Arrange
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = 1,
            WarehouseId = 1,
            Lines = [new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = 0, ReceivedQuantity = 10 }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_PO_LINE");
    }

    [Test]
    public void Validate_ZeroReceivedQuantityInLine_ReturnsInvalid()
    {
        // Arrange
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = 1,
            WarehouseId = 1,
            Lines = [new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = 1, ReceivedQuantity = 0 }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_QUANTITY");
    }

    [Test]
    public void Validate_BatchNumberTooLong_ReturnsInvalid()
    {
        // Arrange
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = 1,
            WarehouseId = 1,
            Lines = [new CreateGoodsReceiptLineRequest { PurchaseOrderLineId = 1, ReceivedQuantity = 10, BatchNumber = new string('B', 51) }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_BATCH_NUMBER");
    }

    [Test]
    public void Validate_FutureManufacturingDate_ReturnsInvalid()
    {
        // Arrange
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = 1,
            WarehouseId = 1,
            Lines = [new CreateGoodsReceiptLineRequest
            {
                PurchaseOrderLineId = 1,
                ReceivedQuantity = 10,
                ManufacturingDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1))
            }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_MANUFACTURING_DATE");
    }

    [Test]
    public void Validate_ExpiryDateBeforeManufacturingDate_ReturnsInvalid()
    {
        // Arrange
        DateOnly manufacturingDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-30));
        CreateGoodsReceiptRequest request = new()
        {
            PurchaseOrderId = 1,
            WarehouseId = 1,
            Lines = [new CreateGoodsReceiptLineRequest
            {
                PurchaseOrderLineId = 1,
                ReceivedQuantity = 10,
                ManufacturingDate = manufacturingDate,
                ExpiryDate = manufacturingDate.AddDays(-1)
            }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_EXPIRY_DATE");
    }
}

/// <summary>
/// Unit tests for InspectLineRequest validation rules per SDD-PURCH-001 section 3.10.
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class InspectLineRequestValidatorTests
{
    private InspectLineRequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new InspectLineRequestValidator();
    }

    [Test]
    public void Validate_ValidAcceptedStatus_ReturnsValid()
    {
        // Arrange
        InspectLineRequest request = new() { InspectionStatus = "Accepted" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyInspectionStatus_ReturnsInvalid()
    {
        // Arrange
        InspectLineRequest request = new() { InspectionStatus = "" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_INSPECTION_STATUS");
    }

    [Test]
    public void Validate_InvalidInspectionStatus_ReturnsInvalid()
    {
        // Arrange
        InspectLineRequest request = new() { InspectionStatus = "Pending" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_INSPECTION_STATUS");
    }

    [Test]
    public void Validate_InspectionNoteTooLong_ReturnsInvalid()
    {
        // Arrange
        InspectLineRequest request = new() { InspectionStatus = "Rejected", InspectionNote = new string('N', 2001) };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_INSPECTION_NOTE");
    }
}

/// <summary>
/// Unit tests for CreateSupplierReturnRequest validation rules per SDD-PURCH-001 section 3.11-3.12.
/// </summary>
[TestFixture]
[Category("SDD-PURCH-001")]
public sealed class CreateSupplierReturnRequestValidatorTests
{
    private CreateSupplierReturnRequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new CreateSupplierReturnRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_ReturnsValid()
    {
        // Arrange
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = 1,
            Reason = "Defective parts",
            Lines = [new CreateSupplierReturnLineRequest { ProductId = 1, WarehouseId = 1, Quantity = 5 }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_ZeroSupplierId_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = 0,
            Reason = "Defective",
            Lines = [new CreateSupplierReturnLineRequest { ProductId = 1, WarehouseId = 1, Quantity = 5 }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_SUPPLIER");
    }

    [Test]
    public void Validate_EmptyReason_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = 1,
            Reason = "",
            Lines = [new CreateSupplierReturnLineRequest { ProductId = 1, WarehouseId = 1, Quantity = 5 }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_RETURN_REASON");
    }

    [Test]
    public void Validate_ReasonTooLong_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = 1,
            Reason = new string('R', 501),
            Lines = [new CreateSupplierReturnLineRequest { ProductId = 1, WarehouseId = 1, Quantity = 5 }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_RETURN_REASON");
    }

    [Test]
    public void Validate_EmptyLines_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = 1,
            Reason = "Defective",
            Lines = []
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "RETURN_MUST_HAVE_LINES");
    }

    [Test]
    public void Validate_ZeroProductIdInLine_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = 1,
            Reason = "Defective",
            Lines = [new CreateSupplierReturnLineRequest { ProductId = 0, WarehouseId = 1, Quantity = 5 }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_PRODUCT");
    }

    [Test]
    public void Validate_ZeroWarehouseIdInLine_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = 1,
            Reason = "Defective",
            Lines = [new CreateSupplierReturnLineRequest { ProductId = 1, WarehouseId = 0, Quantity = 5 }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_WAREHOUSE");
    }

    [Test]
    public void Validate_ZeroQuantityInLine_ReturnsInvalid()
    {
        // Arrange
        CreateSupplierReturnRequest request = new()
        {
            SupplierId = 1,
            Reason = "Defective",
            Lines = [new CreateSupplierReturnLineRequest { ProductId = 1, WarehouseId = 1, Quantity = 0 }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_QUANTITY");
    }
}
