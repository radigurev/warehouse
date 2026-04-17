using FluentAssertions;
using FluentValidation.Results;
using Microsoft.FeatureManagement;
using Moq;
using Warehouse.Customers.API.Validators;
using Warehouse.Infrastructure.Caching;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for CreateAccountRequest validation rules per SDD-CUST-001 section 3.2.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class CreateAccountRequestValidatorTests
{
    private Mock<INomenclatureResolver> _nomenclatureResolverMock = null!;
    private Mock<IFeatureManager> _featureManagerMock = null!;
    private CreateAccountRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _nomenclatureResolverMock = new Mock<INomenclatureResolver>();
        _featureManagerMock = new Mock<IFeatureManager>();
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _validator = new CreateAccountRequestValidator(
            _nomenclatureResolverMock.Object,
            _featureManagerMock.Object);
    }

    [Test]
    public async Task CreateAccountRequestValidator_MissingCurrencyCode_Fails()
    {
        // Arrange
        CreateAccountRequest request = new() { CurrencyCode = "" };

        // Act
        ValidationResult result = await _validator.ValidateAsync(request).ConfigureAwait(false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }

    [Test]
    public async Task CreateAccountRequestValidator_InvalidCurrencyCode_Fails()
    {
        // Arrange
        CreateAccountRequest request = new() { CurrencyCode = "us" };

        // Act
        ValidationResult result = await _validator.ValidateAsync(request).ConfigureAwait(false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }
}
