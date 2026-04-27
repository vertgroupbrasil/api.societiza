using Core.Application.Validators;
using FluentValidation;
using Identity.Shared.Queries.User;

namespace Identity.Application.UseCases.Validators.User;

public sealed class ListAccountancyMembersValidator : PagedQueryValidator<ListAccountancyMembersQuery>
{
    public ListAccountancyMembersValidator()
    {
        RuleFor(x => x.AccountancyId)
            .NotEmpty().WithMessage("AccountancyId is required");

        AddPaginationRules(
            pageNumberSelector: x => x.PageNumber,
            pageSizeSelector: x => x.PageSize);
    }
}
