using FluentValidation;
using Identity.Shared.Commands.Auth;

namespace Identity.Application.UseCases.Validators.Auth;

public sealed class RefreshValidator : AbstractValidator<RefreshCommand>
{
    public RefreshValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}