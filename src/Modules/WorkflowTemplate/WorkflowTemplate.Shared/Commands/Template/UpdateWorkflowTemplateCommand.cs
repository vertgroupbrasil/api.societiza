using Core.Application.Messaging.Commands;

namespace WorkflowTemplate.Shared.Commands.Template;

public sealed record UpdateWorkflowTemplateCommand(
    Guid Id,
    string Name,
    string Description
) : ICommand;