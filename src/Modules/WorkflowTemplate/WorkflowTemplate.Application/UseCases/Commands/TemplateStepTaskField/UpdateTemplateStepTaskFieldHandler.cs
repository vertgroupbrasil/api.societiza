using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Commands.TemplateStepTaskField;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.TemplateStepTaskField;

public class UpdateTemplateStepTaskFieldHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<UpdateTemplateStepTaskFieldCommand>
{
    public async Task<Result> Handle(UpdateTemplateStepTaskFieldCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.TemplateId, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;
        template.UpdateTaskField(
            request.StepId,
            request.TaskId,
            request.FieldId,
            request.Label,
            WorkflowTemplateTypeParsers.ParseFieldType(request.FieldType),
            request.Options,
            request.Order);

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}
