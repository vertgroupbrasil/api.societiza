using FluentValidation;
using Identity.Domain.Enumerations;
using Identity.Shared.Commands;

namespace Identity.Application.UseCases.Validators;

public sealed class InviteUserValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be valid")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(role => UserRole.TryFromName(role, out _))
            .WithMessage($"Role must be one of: {string.Join(", ", UserRole.List.Select(r => r.Name))}");

        RuleFor(x => x.AccountancyId)
            .NotNull().WithMessage("AccountancyId is required")
            .Must(id => id != Guid.Empty).WithMessage("AccountancyId must be a valid GUID")
            .When(x => !UserRole.TryFromName(x.Role, out var role) || role != UserRole.SystemAdmin);
    }
}