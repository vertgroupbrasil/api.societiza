using Core.Application.Messaging.Commands;
using Core.Shared.Response;

namespace WorkflowTemplate.Shared.Commands.Template;

public sealed record CreateWorkflowTemplateCommand(
    string Name,
    string Description
) : ICommand<IdentifierResponse>;