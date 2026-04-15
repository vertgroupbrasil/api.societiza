using FluentValidation;
using WorkflowProcess.Shared.Commands.ProcessStep.Fields;

namespace WorkflowProcess.Application.UseCases.Validators.ProcessStep.Fields;

public sealed class FillProcessStepFieldValidator : AbstractValidator<FillProcessStepFieldCommand>
{
    public FillProcessStepFieldValidator()
    {
        RuleFor(x => x.ProcessId)
            .NotEmpty().WithMessage("ProcessId is required")
            .Must(id => id != Guid.Empty).WithMessage("ProcessId must be a valid GUID");

        RuleFor(x => x.StepInstanceId)
            .NotEmpty().WithMessage("StepInstanceId is required")
            .Must(id => id != Guid.Empty).WithMessage("StepInstanceId must be a valid GUID");

        RuleFor(x => x.FieldInstanceId)
            .NotEmpty().WithMessage("FieldInstanceId is required")
            .Must(id => id != Guid.Empty).WithMessage("FieldInstanceId must be a valid GUID");

        RuleFor(x => x.Value)
            .NotEmpty().WithMessage("Value is required");
    }
}
