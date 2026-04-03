using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Domain.Errors;
using WorkflowTemplate.Shared.Commands.Template;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.Template;

public class ActivateWorkflowTemplateHandler(
    IWorkflowTemplateApplicationService applicationService) : ICommandHandler<ActivateWorkflowTemplateCommand>
{
    public async Task<Result> Handle(ActivateWorkflowTemplateCommand request, CancellationToken cancellationToken)
    {
        var result = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(request.Id, cancellationToken);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        var template = result.Value;

        if (template.Status != TemplateStatus.Draft)
            return Result.Failure(WorkflowTemplateErrors.InvalidStatusTransition(
                from: template.Status.Name,
                to: TemplateStatus.Active.Name));

        if (!template.Steps.Any(step => step.Tasks.Count > 0))
            return Result.Failure(WorkflowTemplateErrors.ActivationRequiresStepWithTask);

        template.Activate();

        await applicationService.AppendEventsAsync(template, cancellationToken);
        return Result.Success();
    }
}
