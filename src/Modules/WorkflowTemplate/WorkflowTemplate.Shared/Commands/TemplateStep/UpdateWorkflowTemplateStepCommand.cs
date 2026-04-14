using Core.Application.Messaging.Commands;

namespace WorkflowTemplate.Shared.Commands.TemplateStep;

public sealed record UpdateWorkflowTemplateStepCommand(
    Guid TemplateId,
    Guid StepId,
    string Title,
    string Description,
    int Order,
    List<string> AppliesTo
) : ICommand;