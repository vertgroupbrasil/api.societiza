using FluentValidation;
using WorkflowProcess.Domain.Enumerations;
using WorkflowProcess.Shared.Commands.Process;

namespace WorkflowProcess.Application.UseCases.Validators.Process;

public sealed class CreateWorkflowProcessValidator : AbstractValidator<CreateWorkflowProcessCommand>
{
    public CreateWorkflowProcessValidator()
    {
        RuleFor(x => x.AccountancyId)
            .NotEmpty().WithMessage("AccountancyId is required")
            .Must(id => id != Guid.Empty).WithMessage("AccountancyId must be a valid GUID");

        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("TemplateId is required")
            .Must(id => id != Guid.Empty).WithMessage("TemplateId must be a valid GUID");

        RuleFor(x => x.ProcessType)
            .NotEmpty().WithMessage("ProcessType is required")
            .Must(type => ProcessType.TryFromName(type, out _)).WithMessage("ProcessType is not valid");

        RuleFor(x => x.TargetClient)
            .NotEmpty().WithMessage("TargetClient is required");
    }
}
