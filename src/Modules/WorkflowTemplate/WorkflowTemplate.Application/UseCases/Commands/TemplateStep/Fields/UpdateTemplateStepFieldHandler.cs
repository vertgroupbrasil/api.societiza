using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStep.Fields;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStep.Fields;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

public class UpdateTemplateStepFieldHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<UpdateTemplateStepFieldCommand>
{
    public async Task<Result> Handle(UpdateTemplateStepFieldCommand request, CancellationToken cancellationToken)
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

        if (!step.Fields.Any(field => field.Id == request.FieldId))
            return Result.Failure(WorkflowTemplateErrors.StepFieldNotFound(request.FieldId));

        var normalizedLabel = request.Label.Trim();

        if (step.Fields.Any(field => field.Id != request.FieldId && string.Equals(field.Label.Trim(), normalizedLabel, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure(WorkflowTemplateErrors.StepFieldLabelAlreadyExists(request.Label));

        var clampedOrder = Math.Clamp(request.Order, 1, step.Fields.Count);

        template.UpdateStepField(
            request.StepId,
            request.FieldId,
            request.Label,
            FieldType.FromName(request.FieldType),
            request.Options,
            clampedOrder);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}