using FluentValidation;
using WorkflowProcess.Shared.Commands.ProcessStepTask;

namespace WorkflowProcess.Application.UseCases.Validators.ProcessStepTask;

public sealed class SkipProcessTaskValidator : AbstractValidator<SkipProcessTaskCommand>
{
    public SkipProcessTaskValidator()
    {
        RuleFor(x => x.ProcessId)
            .NotEmpty().WithMessage("ProcessId is required")
            .Must(id => id != Guid.Empty).WithMessage("ProcessId must be a valid GUID");

        RuleFor(x => x.StepInstanceId)
            .NotEmpty().WithMessage("StepInstanceId is required")
            .Must(id => id != Guid.Empty).WithMessage("StepInstanceId must be a valid GUID");

        RuleFor(x => x.TaskInstanceId)
            .NotEmpty().WithMessage("TaskInstanceId is required")
            .Must(id => id != Guid.Empty).WithMessage("TaskInstanceId must be a valid GUID");
    }
}