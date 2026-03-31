using Core.Application.Messaging.Commands;
using Core.Shared.Response;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
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
