using Core.Application.Messaging.Commands;
using Core.Shared.Response;

namespace WorkflowTemplate.Shared.Commands.TemplateStepField;

public sealed record AddTemplateStepFieldCommand(
    Guid TemplateId,
    Guid StepId,
    string Label,
    string FieldType,
    string? Options
) : ICommand<IdentifierResponse>;