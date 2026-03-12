using Core.Application.Messaging.Commands;

namespace WorkflowTemplate.Shared.Commands.TemplateStepTaskField;

public sealed record RemoveTemplateStepTaskFieldCommand(
    Guid TemplateId,
    Guid StepId,
    Guid TaskId,
    Guid FieldId
) : ICommand;
