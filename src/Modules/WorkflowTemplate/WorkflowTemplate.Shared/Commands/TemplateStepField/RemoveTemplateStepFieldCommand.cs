using Core.Application.Messaging.Commands;

namespace WorkflowTemplate.Shared.Commands.TemplateStepField;

public sealed record RemoveTemplateStepFieldCommand(
    Guid TemplateId,
    Guid StepId,
    Guid FieldId
) : ICommand;