using FluentAssertions;
using FluentValidation.Results;
using Microsoft.FeatureManagement;
using Moq;
using Warehouse.Fulfillment.API.Validators;
using Warehouse.Infrastructure.Caching;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Tests.Unit.Validators;

#region CreateSalesOrderRequestValidator

/// <summary>
/// Unit tests for <see cref="CreateSalesOrderRequestValidator"/> per SDD-FULF-001 section 3.1-3.2.
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
public sealed class CreateSalesOrderRequestValidatorTests
{
    private Mock<INomenclatureResolver> _mockNomenclatureResolver = null!;
    private Mock<IFeatureManager> _mockFeatureManager = null!;
    private CreateSalesOrderRequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _mockNomenclatureResolver = new Mock<INomenclatureResolver>();
        _mockFeatureManager = new Mock<IFeatureManager>();
        _sut = new CreateSalesOrderRequestValidator(_mockNomenclatureResolver.Object, _mockFeatureManager.Object);
    }

    [Test]
    public async Task Validate_ValidRequest_ReturnsValid()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 1,
            CurrencyCode = "USD",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 1, OrderedQuantity = 10m, UnitPrice = 5.99m }]
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task Validate_ZeroCustomerId_ReturnsInvalid()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 0,
            CustomerAccountId = 1,
            CurrencyCode = "USD",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 1, OrderedQuantity = 10m, UnitPrice = 5.99m }]
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_CUSTOMER");
    }

    [Test]
    public async Task Validate_ZeroWarehouseId_ReturnsInvalid()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 1,
            CurrencyCode = "USD",
            WarehouseId = 0,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 1, OrderedQuantity = 10m, UnitPrice = 5.99m }]
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_WAREHOUSE");
    }

    [Test]
    public async Task Validate_PastShipDate_ReturnsInvalid()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 1,
            CurrencyCode = "USD",
            WarehouseId = 1,
            RequestedShipDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)),
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 1, OrderedQuantity = 10m, UnitPrice = 5.99m }]
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_SHIP_DATE");
    }

    [Test]
    public async Task Validate_EmptyShippingCountryCode_ReturnsInvalid()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 1,
            CurrencyCode = "USD",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 1, OrderedQuantity = 10m, UnitPrice = 5.99m }]
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_SHIPPING_ADDRESS");
    }

    [Test]
    public async Task Validate_EmptyLines_ReturnsInvalid()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 1,
            CurrencyCode = "USD",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = []
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SO_MUST_HAVE_LINES");
    }

    [Test]
    public async Task Validate_LineWithZeroProductId_ReturnsInvalid()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 1,
            CurrencyCode = "USD",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 0, OrderedQuantity = 10m, UnitPrice = 5.99m }]
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_PRODUCT");
    }

    [Test]
    public async Task Validate_LineWithZeroQuantity_ReturnsInvalid()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 1,
            CurrencyCode = "USD",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 1, OrderedQuantity = 0m, UnitPrice = 5.99m }]
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_QUANTITY");
    }

    [Test]
    public async Task Validate_LineWithNegativeUnitPrice_ReturnsInvalid()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 1,
            CurrencyCode = "USD",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 1, OrderedQuantity = 10m, UnitPrice = -1m }]
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_UNIT_PRICE");
    }

    /// <summary>CHG-FEAT-007 §3 V9 / §4 E11 — missing CustomerAccountId is rejected.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task CreateSalesOrderValidator_MissingCustomerAccountId_ReturnsInvalid()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 0,
            CurrencyCode = "USD",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 1, OrderedQuantity = 10m, UnitPrice = 5m }]
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SO_INVALID_CUSTOMER_ACCOUNT");
    }

    /// <summary>CHG-FEAT-007 §3 V10 / §4 E12 — missing CurrencyCode is rejected.</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task CreateSalesOrderValidator_MissingCurrencyCode_ReturnsInvalid()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 1,
            CurrencyCode = "",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 1, OrderedQuantity = 10m, UnitPrice = 5m }]
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SO_INVALID_CURRENCY");
    }

    /// <summary>CHG-FEAT-007 §3 V10 — lowercase CurrencyCode is rejected (must be 3 uppercase ASCII letters).</summary>
    [Test]
    [Category("CHG-FEAT-007")]
    public async Task CreateSalesOrderValidator_LowercaseCurrencyCode_ReturnsInvalid()
    {
        // Arrange
        CreateSalesOrderRequest request = new()
        {
            CustomerId = 1,
            CustomerAccountId = 1,
            CurrencyCode = "usd",
            WarehouseId = 1,
            ShippingStreetLine1 = "123 Main St",
            ShippingCity = "Springfield",
            ShippingPostalCode = "62704",
            ShippingCountryCode = "US",
            Lines = [new CreateSalesOrderLineRequest { ProductId = 1, OrderedQuantity = 10m, UnitPrice = 5m }]
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SO_INVALID_CURRENCY");
    }
}

#endregion

#region ConfirmPickRequestValidator

/// <summary>
/// Unit tests for <see cref="ConfirmPickRequestValidator"/> per SDD-FULF-001 section 3.4.
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
public sealed class ConfirmPickRequestValidatorTests
{
    private ConfirmPickRequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new ConfirmPickRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_ReturnsValid()
    {
        // Arrange
        ConfirmPickRequest request = new() { ActualQuantity = 5m };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_ZeroQuantity_ReturnsInvalid()
    {
        // Arrange
        ConfirmPickRequest request = new() { ActualQuantity = 0m };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_QUANTITY");
    }
}

#endregion

#region CreateShipmentRequestValidator

/// <summary>
/// Unit tests for <see cref="CreateShipmentRequestValidator"/> per SDD-FULF-001 section 3.7.
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
public sealed class CreateShipmentRequestValidatorTests
{
    private CreateShipmentRequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new CreateShipmentRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_ReturnsValid()
    {
        // Arrange
        CreateShipmentRequest request = new() { SalesOrderId = 1 };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_ZeroSalesOrderId_ReturnsInvalid()
    {
        // Arrange
        CreateShipmentRequest request = new() { SalesOrderId = 0 };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_SO_REFERENCE");
    }

    [Test]
    public void Validate_NotesExceedMaxLength_ReturnsInvalid()
    {
        // Arrange
        CreateShipmentRequest request = new() { SalesOrderId = 1, Notes = new string('x', 2001) };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_NOTES");
    }
}

#endregion

#region UpdateShipmentStatusRequestValidator

/// <summary>
/// Unit tests for <see cref="UpdateShipmentStatusRequestValidator"/> per SDD-FULF-001 section 3.8.
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
public sealed class UpdateShipmentStatusRequestValidatorTests
{
    private UpdateShipmentStatusRequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new UpdateShipmentStatusRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_ReturnsValid()
    {
        // Arrange
        UpdateShipmentStatusRequest request = new() { Status = "InTransit" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyStatus_ReturnsInvalid()
    {
        // Arrange
        UpdateShipmentStatusRequest request = new() { Status = "" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_SHIPMENT_STATUS");
    }

    [Test]
    public void Validate_TrackingNumberExceedsMaxLength_ReturnsInvalid()
    {
        // Arrange
        UpdateShipmentStatusRequest request = new() { Status = "InTransit", TrackingNumber = new string('T', 101) };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_TRACKING_NUMBER");
    }

    [Test]
    public void Validate_TrackingUrlExceedsMaxLength_ReturnsInvalid()
    {
        // Arrange
        UpdateShipmentStatusRequest request = new() { Status = "InTransit", TrackingUrl = new string('u', 501) };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_TRACKING_URL");
    }
}

#endregion

#region CreateCarrierRequestValidator

/// <summary>
/// Unit tests for <see cref="CreateCarrierRequestValidator"/> per SDD-FULF-001 section 3.9.
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
public sealed class CreateCarrierRequestValidatorTests
{
    private CreateCarrierRequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new CreateCarrierRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_ReturnsValid()
    {
        // Arrange
        CreateCarrierRequest request = new() { Code = "DHL", Name = "DHL Express" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyCode_ReturnsInvalid()
    {
        // Arrange
        CreateCarrierRequest request = new() { Code = "", Name = "DHL Express" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_CARRIER_CODE");
    }

    [Test]
    public void Validate_CodeWithSpecialCharacters_ReturnsInvalid()
    {
        // Arrange
        CreateCarrierRequest request = new() { Code = "DHL @!", Name = "DHL Express" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_CARRIER_CODE");
    }

    [Test]
    public void Validate_EmptyName_ReturnsInvalid()
    {
        // Arrange
        CreateCarrierRequest request = new() { Code = "DHL", Name = "" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Test]
    public void Validate_NameExceedsMaxLength_ReturnsInvalid()
    {
        // Arrange
        CreateCarrierRequest request = new() { Code = "DHL", Name = new string('A', 201) };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_CARRIER_NAME");
    }

    [Test]
    public void Validate_InvalidEmail_ReturnsInvalid()
    {
        // Arrange
        CreateCarrierRequest request = new() { Code = "DHL", Name = "DHL Express", ContactEmail = "not-an-email" };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_EMAIL");
    }
}

#endregion

#region CreateCustomerReturnRequestValidator

/// <summary>
/// Unit tests for <see cref="CreateCustomerReturnRequestValidator"/> per SDD-FULF-001 section 3.11-3.12.
/// </summary>
[TestFixture]
[Category("SDD-FULF-001")]
public sealed class CreateCustomerReturnRequestValidatorTests
{
    private CreateCustomerReturnRequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new CreateCustomerReturnRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_ReturnsValid()
    {
        // Arrange
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = 1,
            Reason = "Damaged in transit",
            Lines = [new CreateCustomerReturnLineRequest { ProductId = 1, WarehouseId = 1, Quantity = 2m }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_ZeroCustomerId_ReturnsInvalid()
    {
        // Arrange
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = 0,
            Reason = "Damaged",
            Lines = [new CreateCustomerReturnLineRequest { ProductId = 1, WarehouseId = 1, Quantity = 2m }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_CUSTOMER");
    }

    [Test]
    public void Validate_EmptyReason_ReturnsInvalid()
    {
        // Arrange
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = 1,
            Reason = "",
            Lines = [new CreateCustomerReturnLineRequest { ProductId = 1, WarehouseId = 1, Quantity = 2m }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Test]
    public void Validate_ReasonExceedsMaxLength_ReturnsInvalid()
    {
        // Arrange
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = 1,
            Reason = new string('R', 501),
            Lines = [new CreateCustomerReturnLineRequest { ProductId = 1, WarehouseId = 1, Quantity = 2m }]
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
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = 1,
            Reason = "Damaged",
            Lines = []
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "RETURN_MUST_HAVE_LINES");
    }

    [Test]
    public void Validate_LineWithZeroProductId_ReturnsInvalid()
    {
        // Arrange
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = 1,
            Reason = "Damaged",
            Lines = [new CreateCustomerReturnLineRequest { ProductId = 0, WarehouseId = 1, Quantity = 2m }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_PRODUCT");
    }

    [Test]
    public void Validate_LineWithZeroWarehouseId_ReturnsInvalid()
    {
        // Arrange
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = 1,
            Reason = "Damaged",
            Lines = [new CreateCustomerReturnLineRequest { ProductId = 1, WarehouseId = 0, Quantity = 2m }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_WAREHOUSE");
    }

    [Test]
    public void Validate_LineWithZeroQuantity_ReturnsInvalid()
    {
        // Arrange
        CreateCustomerReturnRequest request = new()
        {
            CustomerId = 1,
            Reason = "Damaged",
            Lines = [new CreateCustomerReturnLineRequest { ProductId = 1, WarehouseId = 1, Quantity = 0m }]
        };

        // Act
        ValidationResult result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "INVALID_QUANTITY");
    }
}

#endregion
