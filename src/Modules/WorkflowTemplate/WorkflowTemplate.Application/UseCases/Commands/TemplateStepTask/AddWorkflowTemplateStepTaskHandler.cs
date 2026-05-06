using Core.Application.Messaging.Commands;
using Core.Shared.Response;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStepTask;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStepTask;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

public class AddWorkflowTemplateStepTaskHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<AddWorkflowTemplateStepTaskCommand, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(AddWorkflowTemplateStepTaskCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure<IdentifierResponse>(result.Error);

        var template = result.Value;

        if (template.Status != TemplateStatus.Draft)
            return Result.Failure<IdentifierResponse>(WorkflowTemplateErrors.TemplateNotEditable);

        var step = template.Steps.FirstOrDefault(existingStep => existingStep.Id == request.StepId);

        if (step is null)
            return Result.Failure<IdentifierResponse>(WorkflowTemplateErrors.StepNotFound(request.StepId));

        var normalizedTitle = request.Title.Trim();

        if (step.Tasks.Any(task => string.Equals(task.Title.Trim(), normalizedTitle, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure<IdentifierResponse>(WorkflowTemplateErrors.TaskTitleAlreadyExists(request.Title));

        var clampedOrder = Math.Clamp(request.Order, 1, step.Tasks.Count + 1);

        var taskId = template.AddTask(
            request.StepId,
            request.Title,
            request.Description,
            TaskType.FromName(request.Type),
            request.Configuration,
            clampedOrder,
            request.IsOptional);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success(new IdentifierResponse(taskId));
    }
}