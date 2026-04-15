using Core.Application.Messaging.Commands;
using Core.Shared.Response;

namespace WorkflowProcess.Shared.Commands.Process;

public sealed record CreateWorkflowProcessCommand(
    Guid AccountancyId,
    Guid TemplateId,
    string ProcessType,
    string TargetClient
) : ICommand<IdentifierResponse>;