using Core.Application.Messaging.Commands;

namespace WorkflowProcess.Shared.Commands.Process;

public sealed record DeleteWorkflowProcessCommand(Guid Id) : ICommand;
