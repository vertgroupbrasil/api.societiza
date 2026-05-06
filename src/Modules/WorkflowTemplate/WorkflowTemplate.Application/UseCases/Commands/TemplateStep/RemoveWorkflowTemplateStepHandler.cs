using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStep;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStep;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

public class RemoveWorkflowTemplateStepHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<RemoveWorkflowTemplateStepCommand>
{
    public async Task<Result> Handle(RemoveWorkflowTemplateStepCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;

        if (template.Status != TemplateStatus.Draft)
            return Result.Failure(WorkflowTemplateErrors.TemplateNotEditable);

        if (!template.Steps.Any(step => step.Id == request.StepId))
            return Result.Failure(WorkflowTemplateErrors.StepNotFound(request.StepId));

        template.RemoveStep(request.StepId);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}