using FluentValidation;
using Identity.Shared.Queries.User;

namespace Identity.Application.UseCases.Validators.User;

public sealed class ListAccountancyMembersValidator : AbstractValidator<ListAccountancyMembersQuery>
{
    public ListAccountancyMembersValidator()
    {
        RuleFor(x => x.AccountancyId)
            .NotEmpty().WithMessage("AccountancyId is required");
    }
}
