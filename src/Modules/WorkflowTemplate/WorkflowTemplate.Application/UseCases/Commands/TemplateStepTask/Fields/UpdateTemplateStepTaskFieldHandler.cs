using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStepTask.Fields;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStepTask.Fields;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

public class UpdateTemplateStepTaskFieldHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<UpdateTemplateStepTaskFieldCommand>
{
    public async Task<Result> Handle(UpdateTemplateStepTaskFieldCommand request, CancellationToken cancellationToken)
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

        var normalizedLabel = request.Label.Trim();

        if (task.Fields.Any(field => field.Id != request.FieldId && string.Equals(field.Label.Trim(), normalizedLabel, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure(WorkflowTemplateErrors.TaskFieldLabelAlreadyExists(request.Label));

        var clampedOrder = Math.Clamp(request.Order, 1, task.Fields.Count);

        template.UpdateTaskField(
            request.StepId,
            request.TaskId,
            request.FieldId,
            request.Label,
            FieldType.FromName(request.FieldType),
            request.Options,
            clampedOrder);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}