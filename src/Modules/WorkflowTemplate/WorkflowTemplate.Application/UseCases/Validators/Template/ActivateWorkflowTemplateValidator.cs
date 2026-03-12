using FluentValidation;
using WorkflowTemplate.Shared.Commands.Template;

namespace WorkflowTemplate.Application.UseCases.Validators.Template;

public sealed class ActivateWorkflowTemplateValidator : AbstractValidator<ActivateWorkflowTemplateCommand>
{
    public ActivateWorkflowTemplateValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required")
            .Must(id => id != Guid.Empty).WithMessage("Id must be a valid GUID");
    }
}