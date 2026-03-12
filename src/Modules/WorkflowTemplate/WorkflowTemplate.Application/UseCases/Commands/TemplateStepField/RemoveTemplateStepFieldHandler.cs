using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Shared.Commands.TemplateStepField;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStepField;

public class RemoveTemplateStepFieldHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<RemoveTemplateStepFieldCommand>
{
    public async Task<Result> Handle(RemoveTemplateStepFieldCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;
        template.RemoveStepField(request.StepId, request.FieldId);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}