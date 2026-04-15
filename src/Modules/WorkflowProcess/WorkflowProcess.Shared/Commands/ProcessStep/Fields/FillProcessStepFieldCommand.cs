using Core.Application.Messaging.Commands;

namespace WorkflowProcess.Shared.Commands.ProcessStep.Fields;

public sealed record FillProcessStepFieldCommand(
    Guid ProcessId,
    Guid StepInstanceId,
    Guid FieldInstanceId,
    string Value
) : ICommand;
