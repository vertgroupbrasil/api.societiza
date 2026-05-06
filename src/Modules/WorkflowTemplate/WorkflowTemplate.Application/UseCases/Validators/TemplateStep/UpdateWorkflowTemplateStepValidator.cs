using FluentValidation;
using WorkflowTemplate.Shared.Commands.TemplateStep;

namespace WorkflowTemplate.Application.UseCases.Validators.TemplateStep;

public sealed class UpdateWorkflowTemplateStepValidator : AbstractValidator<UpdateWorkflowTemplateStepCommand>
{
    public UpdateWorkflowTemplateStepValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("TemplateId is required")
            .Must(id => id != Guid.Empty).WithMessage("TemplateId must be a valid GUID");

        RuleFor(x => x.StepId)
            .NotEmpty().WithMessage("StepId is required")
            .Must(id => id != Guid.Empty).WithMessage("StepId must be a valid GUID");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(150).WithMessage("Title cannot exceed 150 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Order)
            .GreaterThan(0).WithMessage("Order must be greater than 0");
    }
}