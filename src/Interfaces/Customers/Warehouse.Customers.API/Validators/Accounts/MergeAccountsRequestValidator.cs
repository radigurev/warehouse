using FluentValidation;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Validators;

/// <summary>
/// Validates the merge accounts request payload per SDD-CUST-001 section 2.2.5.
/// </summary>
public sealed class MergeAccountsRequestValidator : AbstractValidator<MergeAccountsRequest>
{
    /// <summary>
    /// Initializes validation rules for account merge.
    /// </summary>
    public MergeAccountsRequestValidator()
    {
        RuleFor(x => x.SourceAccountId)
            .GreaterThan(0).WithErrorCode("INVALID_SOURCE_ACCOUNT").WithMessage("Source account ID must be greater than 0.");

        RuleFor(x => x.TargetAccountId)
            .GreaterThan(0).WithErrorCode("INVALID_TARGET_ACCOUNT").WithMessage("Target account ID must be greater than 0.");

        RuleFor(x => x.SourceAccountId)
            .NotEqual(x => x.TargetAccountId)
            .WithErrorCode("SAME_ACCOUNT_MERGE")
            .WithMessage("Source and target account IDs must be different.");
    }
}
