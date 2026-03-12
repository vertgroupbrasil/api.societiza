using FluentValidation;
using WorkflowTemplate.Shared.Commands.Template;

namespace WorkflowTemplate.Application.UseCases.Validators.Template;

public sealed class UpdateWorkflowTemplateValidator : AbstractValidator<UpdateWorkflowTemplateCommand>
{
    public UpdateWorkflowTemplateValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required")
            .Must(id => id != Guid.Empty).WithMessage("Id must be a valid GUID");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(150).WithMessage("Name cannot exceed 150 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }
}