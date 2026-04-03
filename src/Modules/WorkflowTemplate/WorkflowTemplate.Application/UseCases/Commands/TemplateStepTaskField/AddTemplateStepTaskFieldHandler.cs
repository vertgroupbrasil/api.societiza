using Core.Application.Messaging.Commands;
using Core.Shared.Response;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Domain.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStepTaskField;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStepTaskField;

public class AddTemplateStepTaskFieldHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<AddTemplateStepTaskFieldCommand, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(AddTemplateStepTaskFieldCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure<IdentifierResponse>(result.Error);

        var template = result.Value;
        var step = template.Steps.FirstOrDefault(existingStep => existingStep.Id == request.StepId);

        if (step is null)
            return Result.Failure<IdentifierResponse>(WorkflowTemplateErrors.StepNotFound(request.StepId));

        var task = step.Tasks.FirstOrDefault(existingTask => existingTask.Id == request.TaskId);

        if (task is null)
            return Result.Failure<IdentifierResponse>(WorkflowTemplateErrors.TaskNotFound(request.TaskId));

        if (task.Fields.Any(field => field.Label == request.Label))
            return Result.Failure<IdentifierResponse>(WorkflowTemplateErrors.TaskFieldLabelAlreadyExists(request.Label));

        var fieldId = template.AddTaskField(
            request.StepId,
            request.TaskId,
            request.Label,
            FieldType.FromName(request.FieldType),
            request.Options);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success(new IdentifierResponse(fieldId));
    }
}
