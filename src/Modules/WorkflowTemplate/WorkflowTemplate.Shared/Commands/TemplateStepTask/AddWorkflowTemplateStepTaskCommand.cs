using Core.Application.Messaging.Commands;
using Core.Shared.Response;

namespace WorkflowTemplate.Shared.Commands.TemplateStepTask;

public sealed record AddWorkflowTemplateStepTaskCommand(
    Guid TemplateId,
    Guid StepId,
    string Title,
    string Description,
    string Type,
    string Configuration,
    int Order,
    bool IsOptional
) : ICommand<IdentifierResponse>;