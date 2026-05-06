using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStep;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStep;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

public class UpdateWorkflowTemplateStepHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<UpdateWorkflowTemplateStepCommand>
{
    public async Task<Result> Handle(UpdateWorkflowTemplateStepCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;

        if (template.Status != TemplateStatus.Draft)
            return Result.Failure(WorkflowTemplateErrors.TemplateNotEditable);

        if (!template.Steps.Any(step => step.Id == request.StepId))
            return Result.Failure(WorkflowTemplateErrors.StepNotFound(request.StepId));

        var normalizedTitle = request.Title.Trim();

        if (template.Steps.Any(step => step.Id != request.StepId && string.Equals(step.Title.Trim(), normalizedTitle, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure(WorkflowTemplateErrors.StepTitleAlreadyExists(request.Title));

        var clampedOrder = Math.Clamp(request.Order, 1, template.Steps.Count);

        template.UpdateStep(request.StepId, request.Title, request.Description, clampedOrder);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}