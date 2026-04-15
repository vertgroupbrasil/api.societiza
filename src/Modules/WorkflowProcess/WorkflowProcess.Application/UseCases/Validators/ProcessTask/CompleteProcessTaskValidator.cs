using FluentValidation;
using WorkflowProcess.Shared.Commands.ProcessTask;

namespace WorkflowProcess.Application.UseCases.Validators.ProcessTask;

public sealed class CompleteProcessTaskValidator : AbstractValidator<CompleteProcessTaskCommand>
{
    public CompleteProcessTaskValidator()
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
