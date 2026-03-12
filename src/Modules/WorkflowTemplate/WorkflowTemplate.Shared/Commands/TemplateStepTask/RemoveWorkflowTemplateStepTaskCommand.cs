using Core.Application.Messaging.Commands;

namespace WorkflowTemplate.Shared.Commands.TemplateStepTask;

public sealed record RemoveWorkflowTemplateStepTaskCommand(
    Guid TemplateId,
    Guid StepId,
    Guid TaskId
) : ICommand;