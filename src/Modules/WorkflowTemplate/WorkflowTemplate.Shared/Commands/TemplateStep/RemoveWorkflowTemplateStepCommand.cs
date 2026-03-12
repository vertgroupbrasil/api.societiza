using Core.Application.Messaging.Commands;

namespace WorkflowTemplate.Shared.Commands.TemplateStep;

public sealed record RemoveWorkflowTemplateStepCommand(
    Guid TemplateId,
    Guid StepId
) : ICommand;