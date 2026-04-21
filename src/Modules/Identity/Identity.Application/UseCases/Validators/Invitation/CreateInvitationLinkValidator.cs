using FluentValidation;
using Identity.Domain.Enumerations;
using Identity.Shared.Commands.Invitation;

namespace Identity.Application.UseCases.Validators.Invitation;

public sealed class CreateInvitationLinkValidator : AbstractValidator<CreateInvitationLinkCommand>
{
    public CreateInvitationLinkValidator()
    {
        RuleFor(x => x.TargetRole)
            .NotEmpty().WithMessage("TargetRole is required")
            .Must(role => UserRole.TryFromName(role, out _))
            .WithMessage($"TargetRole must be one of: {string.Join(", ", UserRole.List.Select(r => r.Name))}");

        RuleFor(x => x.AccountancyId)
            .Must(id => id != Guid.Empty).WithMessage("AccountancyId must be a valid GUID")
            .When(x => x.AccountancyId.HasValue);

        RuleFor(x => x.MaxUses)
            .NotNull().WithMessage("MaxUses is required for AccountancyEmployee links")
            .GreaterThanOrEqualTo(1).WithMessage("MaxUses must be at least 1")
            .When(x => x.TargetRole == UserRole.AccountancyEmployee.Name);

        RuleFor(x => x.MaxUses)
            .Null().WithMessage("MaxUses must not be provided for SystemAdmin or AccountancyAdmin links — limit is fixed to 1")
            .When(x => UserRole.TryFromName(x.TargetRole, out var role) && role != UserRole.AccountancyEmployee);
    }
}