using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Shared.Commands.Template;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.Template;

public class UpdateWorkflowTemplateHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<UpdateWorkflowTemplateCommand>
{
    public async Task<Result> Handle(UpdateWorkflowTemplateCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.Id, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;
        template.Update(request.Name, request.Description);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}