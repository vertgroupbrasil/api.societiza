using Accountancy.Shared.Commands;
using Core.Shared.Extensions;
using FluentValidation;

namespace Accountancy.Application.UseCases.Validators;

public sealed class UpdateAccountancyValidator : AbstractValidator<UpdateAccountancyCommand>
{
    public UpdateAccountancyValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required")
            .Must(id => id != Guid.Empty).WithMessage("Id must be a valid GUID");

        RuleFor(x => x.CNPJ)
            .NotEmpty().WithMessage("CNPJ is required")
            .Must(cnpj => cnpj.IsValidCnpj()).WithMessage("CNPJ must be valid");

        RuleFor(x => x.LegalName)
            .NotEmpty().WithMessage("Legal name is required")
            .MaximumLength(150).WithMessage("Legal name cannot exceed 150 characters");

        RuleFor(x => x.TradeName)
            .MaximumLength(150).WithMessage("Trade name cannot exceed 150 characters");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required")
            .MaximumLength(200).WithMessage("Address cannot exceed 200 characters");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required")
            .MaximumLength(2).WithMessage("State must have a maximum of 2 characters");

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required")
            .Must(postalCode => postalCode.IsValidPostalCode()).WithMessage("Postal code must be valid");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required")
            .Must(phone => phone.IsValidPhoneNumber()).WithMessage("Phone number must be valid");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email must be valid")
            .MaximumLength(100).WithMessage("Email cannot exceed 100 characters");
    }
}