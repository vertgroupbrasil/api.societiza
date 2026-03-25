using FluentValidation;
using Identity.Domain.Enumerations;
using Identity.Shared.Commands;

namespace Identity.Application.UseCases.Validators;

public sealed class InviteUserValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(150).WithMessage("Name cannot exceed 150 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be valid")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(role => Enum.TryParse<UserRole>(role, out _))
            .WithMessage($"Role must be one of: {string.Join(", ", Enum.GetNames<UserRole>())}");

        RuleFor(x => x.AccountancyId)
            .NotNull().WithMessage("AccountancyId is required")
            .Must(id => id != Guid.Empty).WithMessage("AccountancyId must be a valid GUID")
            .When(x => !Enum.TryParse<UserRole>(x.Role, out var role) || role != UserRole.SystemAdmin);
    }
}