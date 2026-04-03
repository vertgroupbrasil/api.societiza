using Core.Application.Messaging.Commands;
using Core.Shared.Response;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Domain.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStepTask;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStepTask;

public class AddWorkflowTemplateStepTaskHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<AddWorkflowTemplateStepTaskCommand, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(AddWorkflowTemplateStepTaskCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure<IdentifierResponse>(result.Error);

        var template = result.Value;
        var step = template.Steps.FirstOrDefault(existingStep => existingStep.Id == request.StepId);

        if (step is null)
            return Result.Failure<IdentifierResponse>(WorkflowTemplateErrors.StepNotFound(request.StepId));

        if (step.Tasks.Any(task => task.Title == request.Title))
            return Result.Failure<IdentifierResponse>(WorkflowTemplateErrors.TaskTitleAlreadyExists(request.Title));

        var taskId = template.AddTask(
            request.StepId,
            request.Title,
            request.Description,
            TaskType.FromName(request.Type),
            request.Configuration,
            request.Order,
            request.IsOptional);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success(new IdentifierResponse(taskId));
    }
}
