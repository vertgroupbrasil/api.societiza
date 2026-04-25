using FluentValidation;
using Identity.Shared.Commands.User;

namespace Identity.Application.UseCases.Validators.User;

public sealed class PromoteUserToAccountancyAdminValidator : AbstractValidator<PromoteUserToAccountancyAdminCommand>
{
    public PromoteUserToAccountancyAdminValidator()
    {
        RuleFor(x => x.AccountancyId)
            .NotEmpty().WithMessage("AccountancyId is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
    }
}
