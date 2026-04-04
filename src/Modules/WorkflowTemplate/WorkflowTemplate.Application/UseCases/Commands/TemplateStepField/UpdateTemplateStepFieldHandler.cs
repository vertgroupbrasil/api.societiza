using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStepField;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStepField;

public class UpdateTemplateStepFieldHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<UpdateTemplateStepFieldCommand>
{
    public async Task<Result> Handle(UpdateTemplateStepFieldCommand request, CancellationToken cancellationToken)
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

        if (step.Fields.Any(field => field.Id != request.FieldId && field.Label == request.Label))
            return Result.Failure(WorkflowTemplateErrors.StepFieldLabelAlreadyExists(request.Label));

        template.UpdateStepField(
            request.StepId,
            request.FieldId,
            request.Label,
            FieldType.FromName(request.FieldType),
            request.Options,
            request.Order);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}
