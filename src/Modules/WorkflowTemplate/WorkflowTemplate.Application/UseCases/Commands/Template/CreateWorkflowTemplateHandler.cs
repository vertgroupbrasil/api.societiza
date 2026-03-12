using Core.Application.Messaging.Commands;
using Core.Shared.Response;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Shared.Commands.Template;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.Template;

public class CreateWorkflowTemplateHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<CreateWorkflowTemplateCommand, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(CreateWorkflowTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = WorkflowTemplateAggregate.Create(request.Name, request.Description);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success(new IdentifierResponse(template.Id));
    }
}