using Core.Application.Messaging.Commands;

namespace WorkflowProcess.Shared.Commands.ProcessStepTask;

public sealed record SkipProcessTaskCommand(
    Guid ProcessId,
    Guid StepInstanceId,
    Guid TaskInstanceId
) : ICommand;