using FluentValidation;
using Identity.Shared.Commands.Auth;

namespace Identity.Application.UseCases.Validators.Auth;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be valid");
    }
}