using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStepTask;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStepTask;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

public class RemoveWorkflowTemplateStepTaskHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<RemoveWorkflowTemplateStepTaskCommand>
{
    public async Task<Result> Handle(RemoveWorkflowTemplateStepTaskCommand request, CancellationToken cancellationToken)
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

        if (!step.Tasks.Any(task => task.Id == request.TaskId))
            return Result.Failure(WorkflowTemplateErrors.TaskNotFound(request.TaskId));

        template.RemoveTask(request.StepId, request.TaskId);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}