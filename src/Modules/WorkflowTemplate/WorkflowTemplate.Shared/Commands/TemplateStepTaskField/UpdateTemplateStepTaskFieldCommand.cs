using Core.Application.Messaging.Commands;

namespace WorkflowTemplate.Shared.Commands.TemplateStepTaskField;

public sealed record UpdateTemplateStepTaskFieldCommand(
    Guid TemplateId,
    Guid StepId,
    Guid TaskId,
    Guid FieldId,
    string Label,
    string FieldType,
    string? Options
) : ICommand;
