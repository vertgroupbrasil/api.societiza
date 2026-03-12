using FluentValidation;
using WorkflowTemplate.Shared.Commands.TemplateStepField;

namespace WorkflowTemplate.Application.UseCases.Validators.TemplateStepField;

public sealed class RemoveTemplateStepFieldValidator : AbstractValidator<RemoveTemplateStepFieldCommand>
{
    public RemoveTemplateStepFieldValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("TemplateId is required")
            .Must(id => id != Guid.Empty).WithMessage("TemplateId must be a valid GUID");

        RuleFor(x => x.StepId)
            .NotEmpty().WithMessage("StepId is required")
            .Must(id => id != Guid.Empty).WithMessage("StepId must be a valid GUID");

        RuleFor(x => x.FieldId)
            .NotEmpty().WithMessage("FieldId is required")
            .Must(id => id != Guid.Empty).WithMessage("FieldId must be a valid GUID");
    }
}