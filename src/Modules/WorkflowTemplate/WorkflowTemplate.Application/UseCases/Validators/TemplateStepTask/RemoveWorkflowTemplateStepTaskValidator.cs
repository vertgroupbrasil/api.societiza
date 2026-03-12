using FluentValidation;
using WorkflowTemplate.Shared.Commands.TemplateStepTask;

namespace WorkflowTemplate.Application.UseCases.Validators.TemplateStepTask;

public sealed class RemoveWorkflowTemplateStepTaskValidator : AbstractValidator<RemoveWorkflowTemplateStepTaskCommand>
{
    public RemoveWorkflowTemplateStepTaskValidator()
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
    }
}