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
    IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStepTask> workflowProjection,
    ILogger<ProjectionWorkflowTemplateStepTaskEventHandler> logger) : IProjectionWorkflowTemplateStepTaskEventHandler
{
    public async Task Handle(DomainEvents.WorkflowTemplateStepTaskAdded @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowProjection.ReplaceInsertAsync(new(
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
            await workflowProjection.UpdateFieldsAsync(@event.TaskId, builder => builder
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
            await workflowProjection.DeleteAsync(@event.TaskId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove workflow template step task: {Id}.", @event.TaskId);
            throw new InvalidOperationException($"Failed to remove workflow template step task: {@event.TaskId}.", ex);
        }
    }
}