using Core.Application.EventBus;
using Microsoft.Extensions.Logging;
using WorkflowTemplate.Domain.Events;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;

namespace WorkflowTemplate.Application.UseCases.Events;

public interface IProjectionWorkflowTemplateCascadeDeleteEventHandler :
    IEventHandler<DomainEvents.WorkflowTemplateStepCascadeDeleted>,
    IEventHandler<DomainEvents.WorkflowTemplateStepTaskCascadeDeleted>;

public class ProjectionWorkflowTemplateCascadeDeleteEventHandler(
    IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStep> stepProjection,
    IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStepTask> taskProjection,
    IWorkflowTemplateProjection<ProjectionModel.TemplateStepField> stepFieldProjection,
    IWorkflowTemplateProjection<ProjectionModel.TemplateStepTaskField> taskFieldProjection,
    ILogger<ProjectionWorkflowTemplateCascadeDeleteEventHandler> logger)
    : IProjectionWorkflowTemplateCascadeDeleteEventHandler
{
    public async Task Handle(DomainEvents.WorkflowTemplateStepCascadeDeleted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await stepProjection.DeleteAsync(x => x.Id == @event.StepId, cancellationToken);

            if (@event.FieldIds.Count > 0)
                await stepFieldProjection.DeleteAsync(x => @event.FieldIds.Contains(x.Id), cancellationToken);

            if (@event.TaskIds.Count > 0)
                await taskProjection.DeleteAsync(x => @event.TaskIds.Contains(x.Id), cancellationToken);

            if (@event.TaskFieldIds.Count > 0)
                await taskFieldProjection.DeleteAsync(x => @event.TaskFieldIds.Contains(x.Id), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cascade delete step: {StepId}.", @event.StepId);
            throw new InvalidOperationException($"Failed to cascade delete step: {@event.StepId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowTemplateStepTaskCascadeDeleted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await taskProjection.DeleteAsync(x => x.Id == @event.TaskId, cancellationToken);

            if (@event.TaskFieldIds.Count > 0)
                await taskFieldProjection.DeleteAsync(x => @event.TaskFieldIds.Contains(x.Id), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cascade delete task: {TaskId}.", @event.TaskId);
            throw new InvalidOperationException($"Failed to cascade delete task: {@event.TaskId}.", ex);
        }
    }
}