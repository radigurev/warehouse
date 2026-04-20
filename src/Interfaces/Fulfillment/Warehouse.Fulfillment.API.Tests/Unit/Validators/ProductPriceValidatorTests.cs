using System.Reflection;
using FluentAssertions;
using FluentValidation.Results;
using Warehouse.Fulfillment.API.Validators;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Tests.Unit.Validators;

#region CreateProductPriceRequestValidator

/// <summary>
/// Unit tests for <see cref="CreateProductPriceRequestValidator"/> per CHG-FEAT-007 §3 V1-V5.
/// </summary>
[TestFixture]
[Category("CHG-FEAT-007")]
public sealed class CreateProductPriceRequestValidatorTests
{
    private CreateProductPriceRequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new CreateProductPriceRequestValidator();
    }

    /// <summary>CHG-FEAT-007 §3 V2 — lowercase currency rejected.</summary>
    [Test]
    public async Task CreateValidator_RejectsLowercaseCurrency()
    {
        // Arrange
        CreateProductPriceRequest request = new()
        {
            ProductId = 100,
            CurrencyCode = "usd",
            UnitPrice = 10m
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "FULF_PRICE_INVALID_CURRENCY");
    }

    /// <summary>CHG-FEAT-007 §3 V3 — negative UnitPrice rejected.</summary>
    [Test]
    public async Task CreateValidator_RejectsNegativeUnitPrice()
    {
        // Arrange
        CreateProductPriceRequest request = new()
        {
            ProductId = 100,
            CurrencyCode = "USD",
            UnitPrice = -0.01m
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "FULF_PRICE_INVALID_AMOUNT");
    }

    /// <summary>CHG-FEAT-007 §3 V4 — ValidTo equal to or earlier than ValidFrom rejected.</summary>
    [TestCase("2026-06-01T00:00:00Z", "2026-06-01T00:00:00Z")]
    [TestCase("2026-06-01T00:00:00Z", "2026-05-01T00:00:00Z")]
    public async Task CreateValidator_RejectsValidToBeforeOrEqualValidFrom(string validFromIso, string validToIso)
    {
        // Arrange
        DateTime validFrom = DateTime.Parse(validFromIso, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
        DateTime validTo = DateTime.Parse(validToIso, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);

        CreateProductPriceRequest request = new()
        {
            ProductId = 100,
            CurrencyCode = "USD",
            UnitPrice = 10m,
            ValidFrom = validFrom,
            ValidTo = validTo
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "FULF_PRICE_INVALID_RANGE");
    }

    /// <summary>CHG-FEAT-007 §2.1 — open-ended prices (null ValidFrom and ValidTo) are accepted.</summary>
    [Test]
    public async Task CreateValidator_AcceptsNullValidFromAndValidTo()
    {
        // Arrange
        CreateProductPriceRequest request = new()
        {
            ProductId = 100,
            CurrencyCode = "USD",
            UnitPrice = 10m,
            ValidFrom = null,
            ValidTo = null
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region UpdateProductPriceRequestValidator

/// <summary>
/// Unit tests for <see cref="UpdateProductPriceRequestValidator"/> per CHG-FEAT-007 §3 V3, V4, V7.
/// V7 (ProductId/CurrencyCode immutability) is enforced at the request-shape level: the
/// <see cref="UpdateProductPriceRequest"/> DTO intentionally omits those fields. These tests
/// verify that invariant by reflection.
/// </summary>
[TestFixture]
[Category("CHG-FEAT-007")]
public sealed class UpdateProductPriceRequestValidatorTests
{
    private UpdateProductPriceRequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new UpdateProductPriceRequestValidator();
    }

    /// <summary>
    /// CHG-FEAT-007 §3 V7 — the Update request DTO MUST NOT carry a ProductId field.
    /// Adaptation: since the request cannot structurally transport a new ProductId, the
    /// "reject change" invariant is verified at the DTO-shape level via reflection.
    /// </summary>
    [Test]
    public void UpdateValidator_RejectsProductIdChange()
    {
        // Arrange
        PropertyInfo[] props = typeof(UpdateProductPriceRequest).GetProperties();

        // Act
        bool hasProductId = props.Any(p => p.Name.Equals("ProductId", StringComparison.Ordinal));

        // Assert
        hasProductId.Should().BeFalse(
            "UpdateProductPriceRequest MUST NOT expose ProductId -- immutable after creation per CHG-FEAT-007 §3 V7.");
    }

    /// <summary>
    /// CHG-FEAT-007 §3 V7 — the Update request DTO MUST NOT carry a CurrencyCode field.
    /// Adaptation: same rationale as ProductId — the invariant is enforced at the DTO shape level.
    /// </summary>
    [Test]
    public void UpdateValidator_RejectsCurrencyCodeChange()
    {
        // Arrange
        PropertyInfo[] props = typeof(UpdateProductPriceRequest).GetProperties();

        // Act
        bool hasCurrency = props.Any(p => p.Name.Equals("CurrencyCode", StringComparison.Ordinal));

        // Assert
        hasCurrency.Should().BeFalse(
            "UpdateProductPriceRequest MUST NOT expose CurrencyCode -- immutable after creation per CHG-FEAT-007 §3 V7.");
    }

    /// <summary>CHG-FEAT-007 §3 V3 — update validator also rejects negative price.</summary>
    [Test]
    public async Task UpdateValidator_RejectsNegativeUnitPrice()
    {
        // Arrange
        UpdateProductPriceRequest request = new() { UnitPrice = -1m };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "FULF_PRICE_INVALID_AMOUNT");
    }

    /// <summary>CHG-FEAT-007 §3 V4 — update validator rejects ValidTo &lt;= ValidFrom.</summary>
    [Test]
    public async Task UpdateValidator_RejectsValidToEqualValidFrom()
    {
        // Arrange
        DateTime anchor = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        UpdateProductPriceRequest request = new()
        {
            UnitPrice = 10m,
            ValidFrom = anchor,
            ValidTo = anchor
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "FULF_PRICE_INVALID_RANGE");
    }
}

#endregion
