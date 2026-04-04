using FluentValidation;
using Warehouse.ServiceModel.Requests.Customers;

namespace Warehouse.Customers.API.Validators;

/// <summary>
/// Validates the search customers request payload per SDD-CUST-001 section 2.1.4.
/// </summary>
public sealed class SearchCustomersRequestValidator : AbstractValidator<SearchCustomersRequest>
{
    private static readonly string[] AllowedSortFields = ["name", "code", "createdAtUtc"];

    /// <summary>
    /// Initializes validation rules for customer search.
    /// </summary>
    public SearchCustomersRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithErrorCode("INVALID_PAGE").WithMessage("Page must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithErrorCode("INVALID_PAGE_SIZE").WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.SortBy)
            .Must(sortBy => AllowedSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithErrorCode("INVALID_SORT_BY")
            .WithMessage("SortBy must be one of: name, code, createdAtUtc.")
            .When(x => !string.IsNullOrEmpty(x.SortBy));
    }
}
