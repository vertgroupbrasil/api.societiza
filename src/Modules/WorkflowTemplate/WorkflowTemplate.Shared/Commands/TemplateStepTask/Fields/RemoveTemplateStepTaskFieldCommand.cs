using Core.Application.Messaging.Commands;

namespace WorkflowTemplate.Shared.Commands.TemplateStepTask.Fields;

public sealed record RemoveTemplateStepTaskFieldCommand(
    Guid TemplateId,
    Guid StepId,
    Guid TaskId,
    Guid FieldId
) : ICommand;
