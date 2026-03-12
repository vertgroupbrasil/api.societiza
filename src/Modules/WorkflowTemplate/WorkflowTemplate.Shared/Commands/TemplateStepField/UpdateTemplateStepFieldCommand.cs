using Core.Application.Messaging.Commands;

namespace WorkflowTemplate.Shared.Commands.TemplateStepField;

public sealed record UpdateTemplateStepFieldCommand(
    Guid TemplateId,
    Guid StepId,
    Guid FieldId,
    string Label,
    string FieldType,
    string? Options
) : ICommand;