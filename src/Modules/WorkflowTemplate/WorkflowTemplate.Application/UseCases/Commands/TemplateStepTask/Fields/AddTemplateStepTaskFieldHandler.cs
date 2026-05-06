using Core.Application.Messaging.Commands;
using Core.Shared.Response;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStepTask.Fields;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStepTask.Fields;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

public class AddTemplateStepTaskFieldHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<AddTemplateStepTaskFieldCommand, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(AddTemplateStepTaskFieldCommand request, CancellationToken cancellationToken)
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

        var task = step.Tasks.FirstOrDefault(existingTask => existingTask.Id == request.TaskId);

        if (task is null)
            return Result.Failure<IdentifierResponse>(WorkflowTemplateErrors.TaskNotFound(request.TaskId));

        var normalizedLabel = request.Label.Trim();

        if (task.Fields.Any(field => string.Equals(field.Label.Trim(), normalizedLabel, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure<IdentifierResponse>(WorkflowTemplateErrors.TaskFieldLabelAlreadyExists(request.Label));

        var clampedOrder = Math.Clamp(request.Order, 1, task.Fields.Count + 1);

        var fieldId = template.AddTaskField(
            request.StepId,
            request.TaskId,
            request.Label,
            FieldType.FromName(request.FieldType),
            request.Options,
            clampedOrder);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success(new IdentifierResponse(fieldId));
    }
}