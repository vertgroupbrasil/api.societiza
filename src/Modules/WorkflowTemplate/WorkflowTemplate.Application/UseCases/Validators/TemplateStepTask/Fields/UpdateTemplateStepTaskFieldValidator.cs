using FluentValidation;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Commands.TemplateStepTask.Fields;

namespace WorkflowTemplate.Application.UseCases.Validators.TemplateStepTask.Fields;

public sealed class UpdateTemplateStepTaskFieldValidator : AbstractValidator<UpdateTemplateStepTaskFieldCommand>
{
    public UpdateTemplateStepTaskFieldValidator()
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

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Label is required")
            .MaximumLength(150).WithMessage("Label cannot exceed 150 characters");

        RuleFor(x => x.FieldType)
            .NotEmpty().WithMessage("FieldType is required")
            .Must(type => Domain.Enumerations.FieldType.TryFromName(type, out _))
            .WithMessage($"FieldType must be one of: {string.Join(", ", Domain.Enumerations.FieldType.List.Select(t => t.Name))}");

        RuleFor(x => x.Options)
            .NotEmpty()
            .When(x => x.FieldType == FieldType.Select.Name)
            .WithMessage("Options are required for Select fields.");

        RuleFor(x => x.Options)
            .Null()
            .When(x => x.FieldType != FieldType.Select.Name)
            .WithMessage("Options must be null for non-Select fields.");

        RuleFor(x => x.Order)
            .GreaterThan(0).WithMessage("Order must be greater than 0");
    }
}
