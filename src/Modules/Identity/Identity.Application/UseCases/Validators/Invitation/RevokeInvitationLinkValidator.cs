using FluentValidation;
using Identity.Shared.Commands.Invitation;

namespace Identity.Application.UseCases.Validators.Invitation;

public sealed class RevokeInvitationLinkValidator : AbstractValidator<RevokeInvitationLinkCommand>
{
    public RevokeInvitationLinkValidator()
    {
        RuleFor(x => x.InvitationLinkId)
            .NotEmpty().WithMessage("InvitationLinkId is required")
            .Must(id => id != Guid.Empty).WithMessage("InvitationLinkId must be a valid GUID");
    }
}