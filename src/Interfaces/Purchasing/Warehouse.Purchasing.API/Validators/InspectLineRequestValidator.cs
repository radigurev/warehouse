using FluentValidation;
using Warehouse.ServiceModel.Requests.Purchasing;

namespace Warehouse.Purchasing.API.Validators;

/// <summary>
/// Validates the inspect line request payload per SDD-PURCH-001 section 3.10.
/// </summary>
public sealed class InspectLineRequestValidator : AbstractValidator<InspectLineRequest>
{
    private static readonly string[] ValidStatuses = ["Accepted", "Rejected", "Quarantined"];

    /// <summary>
    /// Initializes validation rules for receiving inspection.
    /// </summary>
    public InspectLineRequestValidator()
    {
        RuleFor(x => x.InspectionStatus)
            .NotEmpty().WithErrorCode("INVALID_INSPECTION_STATUS").WithMessage("Inspection status is required.")
            .Must(s => ValidStatuses.Contains(s)).WithErrorCode("INVALID_INSPECTION_STATUS").WithMessage("Inspection status must be one of: Accepted, Rejected, Quarantined.");

        RuleFor(x => x.InspectionNote)
            .MaximumLength(2000).WithErrorCode("INVALID_INSPECTION_NOTE").WithMessage("Inspection note must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.InspectionNote));
    }
}
