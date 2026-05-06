using Core.Application.Messaging.Commands;
using Core.Shared.Response;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStep.Fields;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStep.Fields;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

public class AddTemplateStepFieldHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<AddTemplateStepFieldCommand, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(AddTemplateStepFieldCommand request, CancellationToken cancellationToken)
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

        var normalizedLabel = request.Label.Trim();

        if (step.Fields.Any(field => string.Equals(field.Label.Trim(), normalizedLabel, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure<IdentifierResponse>(WorkflowTemplateErrors.StepFieldLabelAlreadyExists(request.Label));

        var clampedOrder = Math.Clamp(request.Order, 1, step.Fields.Count + 1);

        var fieldId = template.AddStepField(
            request.StepId,
            request.Label,
            FieldType.FromName(request.FieldType),
            request.Options,
            clampedOrder);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success(new IdentifierResponse(fieldId));
    }
}