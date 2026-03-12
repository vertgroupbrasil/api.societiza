using FluentValidation;
using WorkflowTemplate.Shared.Commands.TemplateStepField;

namespace WorkflowTemplate.Application.UseCases.Validators.TemplateStepField;

public sealed class AddTemplateStepFieldValidator : AbstractValidator<AddTemplateStepFieldCommand>
{
    public AddTemplateStepFieldValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("TemplateId is required")
            .Must(id => id != Guid.Empty).WithMessage("TemplateId must be a valid GUID");

        RuleFor(x => x.StepId)
            .NotEmpty().WithMessage("StepId is required")
            .Must(id => id != Guid.Empty).WithMessage("StepId must be a valid GUID");

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Label is required")
            .MaximumLength(150).WithMessage("Label cannot exceed 150 characters");

        RuleFor(x => x.FieldType)
            .NotEmpty().WithMessage("FieldType is required")
            .Must(type => Enum.TryParse<Domain.Enumerations.FieldType>(type, out _))
            .WithMessage("FieldType must be a valid FieldType (Text, Boolean, Integer, Select, Date)");
    }
}