using Core.Application.Validators;
using Corporate.Shared.Queries.Accountancy;
using FluentValidation;

namespace Corporate.Application.UseCases.Validators.Accountancy;

public class GetAccountanciesValidator : PagedQueryValidator<GetAccountanciesQuery>
{
    public GetAccountanciesValidator()
    {
        AddPaginationRules(
            query => query.PageNumber,
            query => query.PageSize,
            maxPageSize: 100);

        RuleFor(x => x.State)
            .MaximumLength(2)
            .When(x => !string.IsNullOrWhiteSpace(x.State))
            .WithMessage("State must be a 2-letter code (e.g., SP, RJ)");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm))
            .WithMessage("Search term cannot exceed 200 characters");
    }
}