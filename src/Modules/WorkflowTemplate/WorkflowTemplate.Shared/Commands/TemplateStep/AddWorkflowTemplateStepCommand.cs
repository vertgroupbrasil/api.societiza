using Core.Application.Messaging.Commands;
using Core.Shared.Response;

namespace WorkflowTemplate.Shared.Commands.TemplateStep;

public sealed record AddWorkflowTemplateStepCommand(
    Guid TemplateId,
    string Title,
    string Description,
    int Order,
    List<string> AppliesTo
) : ICommand<IdentifierResponse>;