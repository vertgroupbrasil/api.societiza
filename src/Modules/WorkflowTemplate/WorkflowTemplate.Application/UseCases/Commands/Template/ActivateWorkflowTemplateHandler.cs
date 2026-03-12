using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Shared.Commands.Template;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.Template;

public class ActivateWorkflowTemplateHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<ActivateWorkflowTemplateCommand>
{
    public async Task<Result> Handle(ActivateWorkflowTemplateCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.Id, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;
        template.Activate();

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}