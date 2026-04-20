using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Shared.Commands.TemplateStep.Fields;
using WorkflowTemplate.Shared.Errors;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStep.Fields;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

public class RemoveTemplateStepFieldHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<RemoveTemplateStepFieldCommand>
{
    public async Task<Result> Handle(RemoveTemplateStepFieldCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;
        var step = template.Steps.FirstOrDefault(existingStep => existingStep.Id == request.StepId);

        if (step is null)
            return Result.Failure(WorkflowTemplateErrors.StepNotFound(request.StepId));

        if (!step.Fields.Any(field => field.Id == request.FieldId))
            return Result.Failure(WorkflowTemplateErrors.StepFieldNotFound(request.FieldId));

        template.RemoveStepField(request.StepId, request.FieldId);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}