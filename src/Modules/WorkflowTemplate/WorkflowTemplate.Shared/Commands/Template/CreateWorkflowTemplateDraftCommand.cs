using Core.Application.Messaging.Commands;
using Core.Shared.Response;

namespace WorkflowTemplate.Shared.Commands.Template;

public sealed record CreateWorkflowTemplateDraftCommand(Guid Id)
    : ICommand<IdentifierResponse>;
