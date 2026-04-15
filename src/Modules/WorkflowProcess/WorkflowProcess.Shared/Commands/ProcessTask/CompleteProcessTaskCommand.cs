using Core.Application.Messaging.Commands;

namespace WorkflowProcess.Shared.Commands.ProcessTask;

public sealed record CompleteProcessTaskCommand(
    Guid ProcessId,
    Guid StepInstanceId,
    Guid TaskInstanceId
) : ICommand;
