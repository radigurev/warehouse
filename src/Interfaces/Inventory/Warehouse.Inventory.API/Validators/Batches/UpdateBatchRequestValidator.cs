using FluentValidation;
using Warehouse.ServiceModel.Requests.Inventory;

namespace Warehouse.Inventory.API.Validators;

/// <summary>
/// Validates the update batch request payload.
/// </summary>
public sealed class UpdateBatchRequestValidator : AbstractValidator<UpdateBatchRequest>
{
    /// <summary>
    /// Initializes validation rules for batch update.
    /// </summary>
    public UpdateBatchRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithErrorCode("INVALID_NOTES").WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
