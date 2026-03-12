using Core.Application.Messaging.Commands;
using Core.Shared.Response;

namespace WorkflowTemplate.Shared.Commands.TemplateStepTaskField;

public sealed record AddTemplateStepTaskFieldCommand(
    Guid TemplateId,
    Guid StepId,
    Guid TaskId,
    string Label,
    string FieldType,
    string? Options
) : ICommand<IdentifierResponse>;
