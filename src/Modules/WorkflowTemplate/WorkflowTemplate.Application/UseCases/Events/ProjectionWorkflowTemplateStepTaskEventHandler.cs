using Core.Application.EventBus;
using Microsoft.Extensions.Logging;
using WorkflowTemplate.Domain.Events;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;

namespace WorkflowTemplate.Application.UseCases.Events;

public interface IProjectionWorkflowTemplateStepTaskEventHandler :
    IEventHandler<DomainEvents.WorkflowTemplateStepTaskAdded>,
    IEventHandler<DomainEvents.WorkflowTemplateStepTaskUpdated>,
    IEventHandler<DomainEvents.WorkflowTemplateStepTaskRemoved>;

public class ProjectionWorkflowTemplateStepTaskEventHandler(
    IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStepTask> taskProjection,
    IWorkflowTemplateProjection<ProjectionModel.TemplateStepTaskField> taskFieldProjection,
    ILogger<ProjectionWorkflowTemplateStepTaskEventHandler> logger) : IProjectionWorkflowTemplateStepTaskEventHandler
{
    public async Task Handle(DomainEvents.WorkflowTemplateStepTaskAdded @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await taskProjection.ReplaceInsertAsync(new(
                @event.TaskId,
                @event.TemplateId,
                @event.StepId,
                @event.Title,
                @event.Description,
                @event.Type,
                @event.Configuration,
                @event.Order,
                @event.IsOptional,
                @event.CreatedAt,
                null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create workflow template step task: {Id}.", @event.TaskId);
            throw new InvalidOperationException($"Failed to create workflow template step task: {@event.TaskId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowTemplateStepTaskUpdated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await taskProjection.UpdateFieldsAsync(@event.TaskId, builder => builder
                .Set(x => x.Title, @event.Title)
                .Set(x => x.Description, @event.Description)
                .Set(x => x.Type, @event.Type)
                .Set(x => x.Configuration, @event.Configuration)
                .Set(x => x.Order, @event.Order)
                .Set(x => x.IsOptional, @event.IsOptional)
                .Set(x => x.UpdatedAt, @event.UpdatedAt), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update workflow template step task: {Id}.", @event.TaskId);
            throw new InvalidOperationException($"Failed to update workflow template step task: {@event.TaskId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowTemplateStepTaskRemoved @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await taskProjection.DeleteAsync(x => x.Id == @event.TaskId, cancellationToken);

            if (@event.TaskFieldIds.Count > 0)
                await taskFieldProjection.DeleteAsync(x => @event.TaskFieldIds.Contains(x.Id), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove workflow template step task: {Id}.", @event.TaskId);
            throw new InvalidOperationException($"Failed to remove workflow template step task: {@event.TaskId}.", ex);
        }
    }
}