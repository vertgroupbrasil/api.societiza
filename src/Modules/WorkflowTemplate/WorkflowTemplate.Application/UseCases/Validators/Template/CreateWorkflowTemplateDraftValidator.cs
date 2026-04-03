using FluentValidation;
using WorkflowTemplate.Shared.Commands.Template;

namespace WorkflowTemplate.Application.UseCases.Validators.Template;

public sealed class CreateWorkflowTemplateDraftValidator
    : AbstractValidator<CreateWorkflowTemplateDraftCommand>
{
    public CreateWorkflowTemplateDraftValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .Must(id => id != Guid.Empty)
            .WithMessage("Id must be a valid GUID");
    }
}
