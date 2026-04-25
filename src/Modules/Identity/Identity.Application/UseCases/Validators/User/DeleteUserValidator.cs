using FluentValidation;
using Identity.Shared.Commands.User;

namespace Identity.Application.UseCases.Validators.User;

public sealed class DeleteUserValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
    }
}
