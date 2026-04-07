using FluentValidation;
using Warehouse.ServiceModel.Requests.Fulfillment;

namespace Warehouse.Fulfillment.API.Validators;

/// <summary>
/// Validates the create carrier request payload per SDD-FULF-001 section 3.9.
/// </summary>
public sealed class CreateCarrierRequestValidator : AbstractValidator<CreateCarrierRequest>
{
    /// <summary>
    /// Initializes validation rules for carrier creation.
    /// </summary>
    public CreateCarrierRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9-]+$").WithErrorCode("INVALID_CARRIER_CODE").WithMessage("Carrier code is required (1-20 characters, alphanumeric + hyphens).");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithErrorCode("INVALID_CARRIER_NAME").WithMessage("Carrier name is required (1-200 characters).");
        RuleFor(x => x.ContactPhone).MaximumLength(20).WithErrorCode("INVALID_PHONE").When(x => !string.IsNullOrEmpty(x.ContactPhone));
        RuleFor(x => x.ContactEmail).MaximumLength(256).EmailAddress().WithErrorCode("INVALID_EMAIL").When(x => !string.IsNullOrEmpty(x.ContactEmail));
        RuleFor(x => x.WebsiteUrl).MaximumLength(500).WithErrorCode("INVALID_URL").When(x => !string.IsNullOrEmpty(x.WebsiteUrl));
        RuleFor(x => x.TrackingUrlTemplate).MaximumLength(500).WithErrorCode("INVALID_URL_TEMPLATE").When(x => !string.IsNullOrEmpty(x.TrackingUrlTemplate));
        RuleFor(x => x.Notes).MaximumLength(2000).WithErrorCode("INVALID_NOTES").When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
