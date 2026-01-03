using Core.Domain.Enumerations;
using Core.Shared.Extensions;
using Corporate.Shared.Commands.Accountancy;
using FluentValidation;

namespace Corporate.Application.UseCases.Validators.Accountancy;

public class CreateAccountancyValidator : AbstractValidator<CreateAccountancyCommand>
{
    public CreateAccountancyValidator()
    {
        RuleFor(x => x.LegalName)
            .NotEmpty()
            .WithMessage("Legal Name is required")
            .MaximumLength(200)
            .WithMessage("Legal Name must not exceed 200 characters");

        RuleFor(x => x.TradeName)
            .MaximumLength(200)
            .WithMessage("Trade Name must not exceed 200 characters");

        RuleFor(x => x.CNPJ)
            .NotEmpty()
            .WithMessage("CNPJ is required")
            .Must(StringExtensions.IsValidCnpj)
            .WithMessage("CNPJ must be valid and contain exactly 14 digits");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Address is required")
            .MaximumLength(500)
            .WithMessage("Address must not exceed 500 characters");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required")
            .MaximumLength(100)
            .WithMessage("City must not exceed 100 characters");

        RuleFor(x => x.State)
            .NotEmpty()
            .WithMessage("State is required")
            .Length(2)
            .WithMessage("State must be exactly 2 characters");

        RuleFor(x => x.PostalCode)
            .NotEmpty()
            .WithMessage("Postal code is required")
            .Must(StringExtensions.isValidPostalCode)
            .WithMessage("Postal code must be valid and contain exactly 8 digits");

        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("Phone is required")
            .Must(StringExtensions.isValidPhoneNumber)
            .WithMessage("Phone number must be valid with or without DDD");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email must be a valid email address")
            .MaximumLength(200)
            .WithMessage("Email must not exceed 200 characters");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes must not exceed 1000 characters");

        RuleFor(x => x.DatabaseProvider)
            .IsInEnum()
            .WithMessage($"Database Provider must be one of: {string.Join(", ", Enum.GetNames<DatabaseProvider>())}");

        RuleFor(x => x.DatabaseConnectionString)
            .MaximumLength(1024)
            .WithMessage("Database Connection String must not exceed 1024 characters");
    }
}