using Core.Application.Messaging.Commands;

namespace WorkflowProcess.Shared.Commands.ProcessTask;

public sealed record SkipProcessTaskCommand(
    Guid ProcessId,
    Guid StepInstanceId,
    Guid TaskInstanceId
) : ICommand;
