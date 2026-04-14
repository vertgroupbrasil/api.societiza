using FluentValidation;
using WorkflowTemplate.Shared.Commands.TemplateStepTaskField;

namespace WorkflowTemplate.Application.UseCases.Validators.TemplateStepTaskField;

public sealed class RemoveTemplateStepTaskFieldValidator : AbstractValidator<RemoveTemplateStepTaskFieldCommand>
{
    public RemoveTemplateStepTaskFieldValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("TemplateId is required")
            .Must(id => id != Guid.Empty).WithMessage("TemplateId must be a valid GUID");

        RuleFor(x => x.StepId)
            .NotEmpty().WithMessage("StepId is required")
            .Must(id => id != Guid.Empty).WithMessage("StepId must be a valid GUID");

        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("TaskId is required")
            .Must(id => id != Guid.Empty).WithMessage("TaskId must be a valid GUID");

        RuleFor(x => x.FieldId)
            .NotEmpty().WithMessage("FieldId is required")
            .Must(id => id != Guid.Empty).WithMessage("FieldId must be a valid GUID");
    }
}
