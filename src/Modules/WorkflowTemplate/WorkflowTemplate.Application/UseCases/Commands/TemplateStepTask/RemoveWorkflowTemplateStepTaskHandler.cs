using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Shared.Commands.TemplateStepTask;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStepTask;

public class RemoveWorkflowTemplateStepTaskHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<RemoveWorkflowTemplateStepTaskCommand>
{
    public async Task<Result> Handle(RemoveWorkflowTemplateStepTaskCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;
        template.RemoveTask(request.StepId, request.TaskId);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}