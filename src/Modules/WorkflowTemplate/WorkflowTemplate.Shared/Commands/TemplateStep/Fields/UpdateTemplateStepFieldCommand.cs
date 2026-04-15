using Core.Application.Messaging.Commands;

namespace WorkflowTemplate.Shared.Commands.TemplateStep.Fields;

public sealed record UpdateTemplateStepFieldCommand(
    Guid TemplateId,
    Guid StepId,
    Guid FieldId,
    string Label,
    string FieldType,
    string? Options,
    int Order
) : ICommand;