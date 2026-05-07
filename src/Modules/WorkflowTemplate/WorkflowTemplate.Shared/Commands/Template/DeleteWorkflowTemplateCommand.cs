using Core.Application.Messaging.Commands;

namespace WorkflowTemplate.Shared.Commands.Template;

public sealed record DeleteWorkflowTemplateCommand(Guid Id) : ICommand;
