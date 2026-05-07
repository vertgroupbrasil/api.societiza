using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Commands.Template;
using WorkflowTemplate.Shared.Errors;

namespace WorkflowTemplate.Application.UseCases.Commands.Template;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

public class DeleteWorkflowTemplateHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<DeleteWorkflowTemplateCommand>
{
    public async Task<Result> Handle(DeleteWorkflowTemplateCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.Id, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;

        if (template.Status == TemplateStatus.Active)
            return Result.Failure(WorkflowTemplateErrors.InvalidStatusTransition(
                from: template.Status.Name,
                to: "Deleted"));

        template.Delete();

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}
