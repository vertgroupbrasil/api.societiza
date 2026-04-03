using FluentValidation;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Commands.TemplateStepTask;

namespace WorkflowTemplate.Application.UseCases.Validators.TemplateStepTask;

public sealed class AddWorkflowTemplateStepTaskValidator : AbstractValidator<AddWorkflowTemplateStepTaskCommand>
{
    public AddWorkflowTemplateStepTaskValidator()
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

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required")
            .Must(type => WorkflowTemplateTypeParsers.TryParseTaskType(type, out _))
            .WithMessage($"Type must be one of: {string.Join(", ", WorkflowTemplateTypeParsers.SupportedTaskTypes)}");

        RuleFor(x => x.Configuration)
            .NotNull().WithMessage("Configuration is required");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be greater than or equal to 0");
    }
}
