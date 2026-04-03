using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Domain.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStepTask;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStepTask;

public class UpdateWorkflowTemplateStepTaskHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<UpdateWorkflowTemplateStepTaskCommand>
{
    public async Task<Result> Handle(UpdateWorkflowTemplateStepTaskCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;
        var step = template.Steps.FirstOrDefault(existingStep => existingStep.Id == request.StepId);

        if (step is null)
            return Result.Failure(WorkflowTemplateErrors.StepNotFound(request.StepId));

        if (!step.Tasks.Any(task => task.Id == request.TaskId))
            return Result.Failure(WorkflowTemplateErrors.TaskNotFound(request.TaskId));

        if (step.Tasks.Any(task => task.Id != request.TaskId && task.Title == request.Title))
            return Result.Failure(WorkflowTemplateErrors.TaskTitleAlreadyExists(request.Title));

        template.UpdateTask(
            request.StepId,
            request.TaskId,
            request.Title,
            request.Description,
            TaskType.FromName(request.Type),
            request.Configuration,
            request.Order,
            request.IsOptional);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}
