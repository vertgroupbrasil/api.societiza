using Core.Application.Messaging.Commands;

namespace WorkflowTemplate.Shared.Commands.TemplateStepTask;

public sealed record UpdateWorkflowTemplateStepTaskCommand(
    Guid TemplateId,
    Guid StepId,
    Guid TaskId,
    string Title,
    string Description,
    string Type,
    string Configuration,
    int Order,
    bool IsOptional
) : ICommand;