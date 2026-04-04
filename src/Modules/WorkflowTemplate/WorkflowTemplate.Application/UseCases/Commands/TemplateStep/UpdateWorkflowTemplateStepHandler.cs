using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Shared.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStep;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStep;

public class UpdateWorkflowTemplateStepHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<UpdateWorkflowTemplateStepCommand>
{
    public async Task<Result> Handle(UpdateWorkflowTemplateStepCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;

        if (!template.Steps.Any(step => step.Id == request.StepId))
            return Result.Failure(WorkflowTemplateErrors.StepNotFound(request.StepId));

        if (template.Steps.Any(step => step.Id != request.StepId && step.Title == request.Title))
            return Result.Failure(WorkflowTemplateErrors.StepTitleAlreadyExists(request.Title));

        template.UpdateStep(request.StepId, request.Title, request.Description, request.Order);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}