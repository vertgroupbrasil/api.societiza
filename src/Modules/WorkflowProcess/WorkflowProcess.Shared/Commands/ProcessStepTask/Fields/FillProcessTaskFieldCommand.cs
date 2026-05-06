using Core.Application.Messaging.Commands;

namespace WorkflowProcess.Shared.Commands.ProcessStepTask.Fields;

public sealed record FillProcessTaskFieldCommand(
    Guid ProcessId,
    Guid StepInstanceId,
    Guid TaskInstanceId,
    Guid FieldInstanceId,
    string Value
) : ICommand;