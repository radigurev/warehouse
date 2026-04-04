using FluentAssertions;
using FluentValidation.Results;
using Warehouse.Customers.API.Validators;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Tests.Unit.Validators;

/// <summary>
/// Unit tests for MergeAccountsRequest validation rules per SDD-CUST-001 section 3.7.
/// </summary>
[TestFixture]
[Category("SDD-CUST-001")]
public sealed class MergeAccountsRequestValidatorTests
{
    private MergeAccountsRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new MergeAccountsRequestValidator();
    }

    [Test]
    public void MergeAccountsRequestValidator_MissingSourceOrTarget_Fails()
    {
        // Arrange
        MergeAccountsRequest request = new() { SourceAccountId = 0, TargetAccountId = 0 };

        // Act
        ValidationResult result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SourceAccountId");
        result.Errors.Should().Contain(e => e.PropertyName == "TargetAccountId");
    }
}
