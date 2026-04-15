using Core.Application.Messaging.Commands;
using Core.Shared.Response;

namespace WorkflowTemplate.Shared.Commands.TemplateStep.Fields;

public sealed record AddTemplateStepFieldCommand(
    Guid TemplateId,
    Guid StepId,
    string Label,
    string FieldType,
    string? Options,
    int Order
) : ICommand<IdentifierResponse>;