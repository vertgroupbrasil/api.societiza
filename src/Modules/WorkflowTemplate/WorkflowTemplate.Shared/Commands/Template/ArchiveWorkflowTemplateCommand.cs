using Core.Application.Messaging.Commands;

namespace WorkflowTemplate.Shared.Commands.Template;

public sealed record ArchiveWorkflowTemplateCommand(Guid Id) : ICommand;