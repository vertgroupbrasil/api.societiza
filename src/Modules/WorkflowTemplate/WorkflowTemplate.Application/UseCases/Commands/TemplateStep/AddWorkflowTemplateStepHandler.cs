using Core.Application.Messaging.Commands;
using Core.Shared.Response;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Shared.Errors;
using WorkflowTemplate.Shared.Commands.TemplateStep;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStep;

public class AddWorkflowTemplateStepHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<AddWorkflowTemplateStepCommand, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(AddWorkflowTemplateStepCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure<IdentifierResponse>(result.Error);

        var template = result.Value;

        if (template.Steps.Any(step => step.Title == request.Title))
            return Result.Failure<IdentifierResponse>(WorkflowTemplateErrors.StepTitleAlreadyExists(request.Title));

        var stepId = template.AddStep(request.Title, request.Description, request.Order);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success(new IdentifierResponse(stepId));
    }
}