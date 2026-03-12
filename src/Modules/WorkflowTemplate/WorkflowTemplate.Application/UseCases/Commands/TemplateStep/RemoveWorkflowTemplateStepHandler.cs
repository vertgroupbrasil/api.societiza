using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Shared.Commands.TemplateStep;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStep;

public class RemoveWorkflowTemplateStepHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<RemoveWorkflowTemplateStepCommand>
{
    public async Task<Result> Handle(RemoveWorkflowTemplateStepCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;
        template.RemoveStep(request.StepId);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}