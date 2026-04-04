using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStepTaskField;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStepTaskField;

public class UpdateTemplateStepTaskFieldHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<UpdateTemplateStepTaskFieldCommand>
{
    public async Task<Result> Handle(UpdateTemplateStepTaskFieldCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;
        var step = template.Steps.FirstOrDefault(existingStep => existingStep.Id == request.StepId);

        if (step is null)
            return Result.Failure(WorkflowTemplateErrors.StepNotFound(request.StepId));

        var task = step.Tasks.FirstOrDefault(existingTask => existingTask.Id == request.TaskId);

        if (task is null)
            return Result.Failure(WorkflowTemplateErrors.TaskNotFound(request.TaskId));

        if (!task.Fields.Any(field => field.Id == request.FieldId))
            return Result.Failure(WorkflowTemplateErrors.TaskFieldNotFound(request.FieldId));

        if (task.Fields.Any(field => field.Id != request.FieldId && field.Label == request.Label))
            return Result.Failure(WorkflowTemplateErrors.TaskFieldLabelAlreadyExists(request.Label));

        template.UpdateTaskField(
            request.StepId,
            request.TaskId,
            request.FieldId,
            request.Label,
            FieldType.FromName(request.FieldType),
            request.Options,
            request.Order);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}