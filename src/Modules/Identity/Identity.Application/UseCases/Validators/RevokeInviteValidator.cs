using FluentValidation;
using Identity.Shared.Commands;

namespace Identity.Application.UseCases.Validators;

public sealed class RevokeInviteValidator : AbstractValidator<RevokeInviteCommand>
{
    public RevokeInviteValidator()
    {
        RuleFor(x => x.InviteId)
            .NotEmpty().WithMessage("InviteId is required")
            .Must(id => id != Guid.Empty).WithMessage("InviteId must be a valid GUID");
    }
}