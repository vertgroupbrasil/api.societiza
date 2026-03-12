using FluentValidation;
using WorkflowTemplate.Shared.Commands.TemplateStep;

namespace WorkflowTemplate.Application.UseCases.Validators.TemplateStep;

public sealed class RemoveWorkflowTemplateStepValidator : AbstractValidator<RemoveWorkflowTemplateStepCommand>
{
    public RemoveWorkflowTemplateStepValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("TemplateId is required")
            .Must(id => id != Guid.Empty).WithMessage("TemplateId must be a valid GUID");

        RuleFor(x => x.StepId)
            .NotEmpty().WithMessage("StepId is required")
            .Must(id => id != Guid.Empty).WithMessage("StepId must be a valid GUID");
    }
}