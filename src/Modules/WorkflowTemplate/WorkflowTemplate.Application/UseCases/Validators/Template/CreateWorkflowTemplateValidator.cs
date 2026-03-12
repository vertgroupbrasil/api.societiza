using FluentValidation;
using WorkflowTemplate.Shared.Commands.Template;

namespace WorkflowTemplate.Application.UseCases.Validators.Template;

public sealed class CreateWorkflowTemplateValidator : AbstractValidator<CreateWorkflowTemplateCommand>
{
    public CreateWorkflowTemplateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(150).WithMessage("Name cannot exceed 150 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }
}