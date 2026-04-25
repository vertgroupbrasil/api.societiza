using FluentValidation;
using Identity.Shared.Commands.User;

namespace Identity.Application.UseCases.Validators.User;

public sealed class UpdateUserNameValidator : AbstractValidator<UpdateUserNameCommand>
{
    public UpdateUserNameValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("FirstName is required")
            .MaximumLength(80).WithMessage("FirstName cannot exceed 80 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("LastName is required")
            .MaximumLength(80).WithMessage("LastName cannot exceed 80 characters");
    }
}
