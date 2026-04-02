using FluentValidation;
using Identity.Shared.Commands.Auth;

namespace Identity.Application.UseCases.Validators.Auth;

public sealed class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}
