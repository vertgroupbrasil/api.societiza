using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Commands.TemplateStepTask.Fields;
using WorkflowTemplate.Shared.Errors;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStepTask.Fields;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

public class RemoveTemplateStepTaskFieldHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<RemoveTemplateStepTaskFieldCommand>
{
    public async Task<Result> Handle(RemoveTemplateStepTaskFieldCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;

        if (template.Status != TemplateStatus.Draft)
            return Result.Failure(WorkflowTemplateErrors.TemplateNotEditable);

        var step = template.Steps.FirstOrDefault(existingStep => existingStep.Id == request.StepId);

        if (step is null)
            return Result.Failure(WorkflowTemplateErrors.StepNotFound(request.StepId));

        var task = step.Tasks.FirstOrDefault(existingTask => existingTask.Id == request.TaskId);

        if (task is null)
            return Result.Failure(WorkflowTemplateErrors.TaskNotFound(request.TaskId));

        if (!task.Fields.Any(field => field.Id == request.FieldId))
            return Result.Failure(WorkflowTemplateErrors.TaskFieldNotFound(request.FieldId));

        template.RemoveTaskField(request.StepId, request.TaskId, request.FieldId);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}